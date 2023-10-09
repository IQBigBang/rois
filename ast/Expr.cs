using RoisLang.types;
using RoisLang.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.ast
{
    public abstract record Expr
    {
        public TypeRef Ty = TypeRef.UNKNOWN;
        public SourcePos Pos;

        public Expr(SourcePos pos) { Pos = pos; }

        public abstract bool IsValidLhs();
    };

    public record BoolLit(bool Value, SourcePos Pos) : Expr(Pos)
    {
        public override bool IsValidLhs() => false;
    }

    public record IntExpr(int Value, SourcePos Pos) : Expr(Pos)
    {
        public override bool IsValidLhs() => false;
    }

    public record CharLit(char Ch, SourcePos Pos) : Expr(Pos)
    {
        public override bool IsValidLhs() => false;
    }

    public record StrLit(string Text, SourcePos Pos) : Expr(Pos)
    {
        public override bool IsValidLhs() => false;
    }

    public record VarExpr(string Name, SourcePos Pos) : Expr(Pos)
    {
        public VarExpr(string name, TypeRef typ, SourcePos Pos) : this(name, Pos)
        {
            Ty = typ;
        }
        public override bool IsValidLhs() => true;
    }

    public record UnOpExpr(Expr Exp, UnOpExpr.Ops Op, SourcePos Pos) : Expr(Pos)
    {
        public enum Ops
        {
            Not, // !
            Neg,  // -
        }
        public override bool IsValidLhs() => false;
    }

    public record BinOpExpr(Expr Lhs, Expr Rhs, BinOpExpr.Ops Op, SourcePos Pos) : Expr(Pos)
    {
        public enum Ops
        {
            Add,
            Sub,
            Mul,
            CmpEq,
            CmpNe,
            CmpLt,
            CmpGt,
            CmpLe,
            CmpGe,
            And,
            Or,
        }
        public override bool IsValidLhs() => false;
    }

    public record CallExpr(Expr Callee, Expr[] Args, SourcePos Pos) : Expr(Pos)
    {
        public override bool IsValidLhs() => false;
    }

    public record MemberExpr(Expr Object, string MemberName, SourcePos Pos) : Expr(Pos)
    {
        public override bool IsValidLhs() => true;
    }

    public record MethodCallExpr(Expr Object, string methodName, Expr[] Args, SourcePos Pos) : Expr(Pos)
    {
        public override bool IsValidLhs() => false;
    }

    public record ConstructorExpr(ClassType Class, Dictionary<string, Expr> Fields, SourcePos Pos) : Expr(Pos)
    {
        public override bool IsValidLhs() => false;
    }

    public record FailExpr(SourcePos Pos) : Expr(Pos)
    {   /* TODO */
        public override bool IsValidLhs() => false;
    }

    public record CastAsExpr(Expr Value, TypeRef CastType, SourcePos Pos) : Expr(Pos)
    {
        public override bool IsValidLhs() => false;
    }
}
