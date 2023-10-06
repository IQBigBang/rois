using RoisLang.ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RoisLang.ast.MatchStmt;

namespace RoisLang.lower
{
    internal class MatchLowerer
    {
        public static void VisitProgram(ast.Program program)
            => new MatchLowerer()._visitProgram(program);

        private void _visitProgram(ast.Program program)
        {
            foreach (var fn in program.Functions) VisitFunc(fn);
            foreach (var cls in program.Classes)
                foreach (var fn in cls.Methods)
                    VisitFunc(fn);
        }

        private void VisitFunc(Func func)
        {
            func.Body = LowerBlock(func.Body);
        }

        private Stmt[] LowerBlock(Stmt[] block)
        {
            List<Stmt> newBlock = new List<Stmt>();
            foreach (var stmt in block)
            {
                foreach (var newStmt in VisitStmt(stmt))
                    newBlock.Add(newStmt);
            }
            return newBlock.ToArray();
        }

        private IEnumerable<Stmt> VisitStmt(Stmt stmt)
        {
            switch (stmt)
            {
                case ast.DiscardStmt:
                case ast.LetAssignStmt:
                case ast.AssignStmt:
                case ast.ReturnStmt:
                    yield return stmt;
                    yield break;
                case IfStmt ifStmt:
                    ifStmt.Then = LowerBlock(ifStmt.Then);
                    ifStmt.Else = LowerBlock(ifStmt.Else);
                    yield return ifStmt;
                    yield break;
                case WhileStmt whileStmt:
                    whileStmt.Body = LowerBlock(whileStmt.Body);
                    yield return whileStmt;
                    yield break;
                case MatchStmt matchStmt:
                    foreach (var x in LowerMatch(matchStmt)) yield return x;
                    yield break;
                default:
                    throw new NotImplementedException();
            }
        }

        /// <summary>
        /// `match` is lowered into a series of if-else-elseifs
        /// </summary>
        private IEnumerable<Stmt> LowerMatch(MatchStmt stmt)
        {
            var scrName = "__scr" + Random.Shared.Next(1000, 10000);
            // TODO: failure case
            var scrNameExpr = new LetAssignStmt(scrName, stmt.Scrutinee);
            var compiledCasesList = new List<(Expr, Stmt[])>();
            foreach (var case_ in stmt.Cases)
                compiledCasesList.Add(LowerCase(case_.Item1, scrName, stmt.Scrutinee.Ty!, case_.Item2));
            var compiledCases = IfStmt.Build(compiledCasesList, new DiscardStmt(new FailExpr(stmt.Scrutinee.Pos)));
            yield return scrNameExpr;
            yield return compiledCases;
        }

        // returns (condition, amendedBody)
        private (Expr, Stmt[]) LowerCase(Patt patt, string scrName, types.TypeRef scrTy, Stmt[] body)
        {
            Expr cond = patt switch
            {
                AnyPatt or NamePatt => new BoolLit(true, patt.Pos), // no checking
                // scr == int
                IntLitPatt il => new BinOpExpr(new VarExpr(scrName, scrTy, il.Pos), new IntExpr(il.Val, il.Pos), BinOpExpr.Ops.CmpEq, il.Pos),
                _ => throw new NotImplementedException(),
            };
            cond.Ty = types.TypeRef.BOOL;
            // ammend the body if needed
            if (patt is NamePatt namePatt)
                body = body.Prepend(
                    new LetAssignStmt(namePatt.Name, new VarExpr(scrName, scrTy, patt.Pos))
                    ).ToArray();
            return (cond, body);
        }
    }
}
