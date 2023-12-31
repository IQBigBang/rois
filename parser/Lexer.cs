﻿using RoisLang.utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.parser
{
    public enum Token
    {
        // EndOfInput
        Eoi,
        Indent,
        Dedent,
        Error,
        // one (or more) newlines
        Nl,
        // base-10 integer
        Int,
        // symbol (name)
        Sym,
        StrLit,
        CharLit,
        // '='
        Assign,
        // (
        LParen,
        // )
        RParen,
        // ,
        Comma,
        // :
        Colon,
        // +
        Plus,
        // *
        Star,
        // -
        Minus,
        // ->
        Arrow,
        // ==
        Equal,
        // !=
        NotEqual,
        // <
        Lower,
        // <=
        LowerEqual,
        // >
        Greater,
        // >=
        GreaterEqual,
        // .
        Dot,
        // !
        ExclMark,
        // the 'let' keyword
        KwLet,
        // the 'def' keyword
        KwDef,
        // the 'return' keyword
        KwReturn,
        KwIf,
        KwElse,
        KwTrue,
        KwFalse,
        KwClass,
        KwVal,
        KwExtern,
        KwNew,
        KwFun,
        KwInclude,
        KwWhile,
        KwMatch,
        KwAs,
        KwAnd,
        KwOr,
        KwEnum,
        KwCase,
    }

    public class Lexer
    {
        private string Source;
        private List<int> Newlines = new List<int>();
        private int Pos = 0;
        private List<Superpower.Model.Token<Token>> tokens = new List<Superpower.Model.Token<Token>>();
        private int currentIndent = 0;

        public static List<Superpower.Model.Token<Token>> TokenizeString(string s)
        {
            Lexer l = new() { Source = s };
            l.GenerateNewlinePositions();
            l.LexAll();
            return l.tokens;
        }

        private void GenerateNewlinePositions()
        {
            for (int i = 0; i < Source.Length - 1; i++)
            {
                if (Source[i] == '\n')
                {
                    if (Source[i + 1] == '\r') i++;
                    Newlines.Add(i);
                }
            }
        }

        private Superpower.Model.TextSpan Span(int length)
        {
            // find where the current line starts
            int lineNum = 0;
            for (;lineNum < Newlines.Count; lineNum++)
            {
                if (Newlines[lineNum] > Pos)
                    break;
            }
            var lineStart = lineNum == 0 ? 0 : Newlines[lineNum - 1];
            var col = Pos - lineStart;
            return new Superpower.Model.TextSpan(Source, new Superpower.Model.Position(Pos, lineNum+1, col), length);
        }

        private Superpower.Model.Token<Token> SimpleToken(Token type, int length)
        {
            var span = Span(length);
            Pos += length;
            return new Superpower.Model.Token<Token>(type, span);
        }

        private void LexAll()
        {
            while (Pos < Source.Length)
            {
                char ch = Source[Pos];
                // skip WS
                if (ch == ' ') { Pos++; continue; }
                else if (ch == '\n' || ch == '\r')
                    LexNl();
                // operators
                else if (ch == '=')
                {
                    if (Pos + 1 < Source.Length && Source[Pos + 1] == '=')
                        tokens.Add(SimpleToken(Token.Equal, 2));
                    else
                        tokens.Add(SimpleToken(Token.Assign, 1));
                }
                else if (ch == '!')
                {
                    if (Pos + 1 < Source.Length && Source[Pos + 1] == '=')
                        tokens.Add(SimpleToken(Token.NotEqual, 2));
                    else
                        tokens.Add(SimpleToken(Token.ExclMark, 1));
                }
                else if (ch == '(')
                    tokens.Add(SimpleToken(Token.LParen, 1));
                else if (ch == ')')
                    tokens.Add(SimpleToken(Token.RParen, 1));
                else if (ch == ':')
                    tokens.Add(SimpleToken(Token.Colon, 1));
                else if (ch == ',')
                    tokens.Add(SimpleToken(Token.Comma, 1));
                else if (ch == '+')
                    tokens.Add(SimpleToken(Token.Plus, 1));
                else if (ch == '*')
                    tokens.Add(SimpleToken(Token.Star, 1));
                else if (ch == '-')
                {
                    if (Pos + 1 < Source.Length && Source[Pos + 1] == '>')
                        tokens.Add(SimpleToken(Token.Arrow, 2));
                    else
                        tokens.Add(SimpleToken(Token.Minus, 1));
                }
                else if (ch == '<')
                {
                    if (Pos + 1 < Source.Length && Source[Pos + 1] == '=')
                        tokens.Add(SimpleToken(Token.LowerEqual, 2));
                    else
                        tokens.Add(SimpleToken(Token.Lower, 1));
                }
                else if (ch == '>')
                {
                    if (Pos + 1 < Source.Length && Source[Pos + 1] == '=')
                        tokens.Add(SimpleToken(Token.GreaterEqual, 2));
                    else
                        tokens.Add(SimpleToken(Token.Greater, 1));
                }
                else if (ch == '.')
                    tokens.Add(SimpleToken(Token.Dot, 1));
                else if (ch == '/' && Pos + 1 < Source.Length && Source[Pos + 1] == '*')
                    LexComment();
                // symbols and numbers
                else if (char.IsDigit(ch))
                    LexInt();
                else if (char.IsLetter(ch) || ch == '_')
                    LexSymbol();
                else if (ch == '"')
                    LexStrLit();
                else if (ch == '\'')
                    LexChar();
                else
                {
                    tokens.Add(SimpleToken(Token.Error, 1));
                    return;
                }

            }
            // if there's not a newline add the end, add it
            if (tokens.LastOrDefault().Kind != Token.Nl) tokens.Add(new Superpower.Model.Token<Token>(Token.Nl, Span(0)));
            // at the end, before EOI, emit a sufficient amount of dedents
            while (currentIndent > 0)
            {
                tokens.Add(new Superpower.Model.Token<Token>(Token.Dedent, Span(0)));
                currentIndent--;
            }
            tokens.Add(new Superpower.Model.Token<Token>(Token.Eoi, Span(0)));
        }

        private void LexComment()
        {
            // first two characters are '/*'
            int lenComment = 2;
            while (true)
            {
                if ((Pos + lenComment + 1) >= Source.Length)
                {
                    tokens.Add(SimpleToken(Token.Error, 1)); // unfinished comment
                    return;
                }
                if (Source[Pos + lenComment] == '*' && Source[Pos + lenComment + 1] == '/')
                {   // Finished
                    Pos += lenComment + 2;
                    return;
                }
                lenComment++;
            }
        }

        // tokenize a newline + indentation
        private void LexNl()
        {
            int lenNl = 0;
            while ((Pos + lenNl) < Source.Length && (Source[Pos + lenNl] == '\n' || Source[Pos + lenNl] == '\r'))
                lenNl++;
            tokens.Add(new Superpower.Model.Token<Token>(Token.Nl, Span(lenNl)));
            Pos += lenNl;
            // now the indentation
            int indentSpaces = 0;
            while (Pos < Source.Length && (Source[Pos] == ' ' || Source[Pos] == '\t'))
            {
                if (Source[Pos] == '\t') indentSpaces += 4;
                else indentSpaces += 1;
                Pos++;
            }
            int indentLevel = (indentSpaces + 3) / 4; // this is integer division which rounds up
            while (indentLevel < currentIndent)
            {
                tokens.Add(new Superpower.Model.Token<Token>(Token.Dedent, Span(0)));
                currentIndent--;
            }
            while (indentLevel > currentIndent)
            {
                tokens.Add(new Superpower.Model.Token<Token>(Token.Indent, Span(0)));
                currentIndent++;
            }
        }

        private void LexChar()
        {
            // Source[Pos] is '
            if (Pos + 2 >= Source.Length || Source[Pos + 2] != '\'')
                throw CompilerError.ParseErr("Nonterminated char literal", new SourcePos(Span(1).Position.Line, Span(1).Position.Column));
            tokens.Add(SimpleToken(Token.CharLit, 3));
        }

        private void LexStrLit()
        {
            int len = 1;
            while (true)
            {
                if (Pos + len >= Source.Length)
                    throw CompilerError.ParseErr("Nonterminated string", new SourcePos(Span(1).Position.Line, Span(1).Position.Column));
                if (Source[Pos + len] == '"')
                    break;
                len++;
            }
            // end-of-string
            tokens.Add(SimpleToken(Token.StrLit, len + 1));
        }

        private void LexInt()
        {
            int len = 0;
            while ((Pos + len) < Source.Length && char.IsDigit(Source[Pos + len]))
                len++;
            tokens.Add(SimpleToken(Token.Int, len));
        }

        private void LexSymbol()
        {
            int len = 0;
            while ((Pos + len) < Source.Length && (char.IsLetterOrDigit(Source[Pos + len]) || Source[Pos + len] == '_'))
                len++;
            string s = Source.Substring(Pos, len);
            if (s == "let")
                tokens.Add(SimpleToken(Token.KwLet, 3));
            else if (s == "def")
                tokens.Add(SimpleToken(Token.KwDef, 3));
            else if (s == "return")
                tokens.Add(SimpleToken(Token.KwReturn, 6));
            else if (s == "true")
                tokens.Add(SimpleToken(Token.KwTrue, 4));
            else if (s == "false")
                tokens.Add(SimpleToken(Token.KwFalse, 5));
            else if (s == "if")
                tokens.Add(SimpleToken(Token.KwIf, 2));
            else if (s == "else")
                tokens.Add(SimpleToken(Token.KwElse, 4));
            else if (s == "class")
                tokens.Add(SimpleToken(Token.KwClass, 5));
            else if (s == "val")
                tokens.Add(SimpleToken(Token.KwVal, 3));
            else if (s == "extern")
                tokens.Add(SimpleToken(Token.KwExtern, 6));
            else if (s == "new")
                tokens.Add(SimpleToken(Token.KwNew, 3));
            else if (s == "fun")
                tokens.Add(SimpleToken(Token.KwFun, 3));
            else if (s == "include")
                tokens.Add(SimpleToken(Token.KwInclude, 7));
            else if (s == "while")
                tokens.Add(SimpleToken(Token.KwWhile, 5));
            else if (s == "match")
                tokens.Add(SimpleToken(Token.KwMatch, 5));
            else if (s == "as")
                tokens.Add(SimpleToken(Token.KwAs, 2));
            else if (s == "and")
                tokens.Add(SimpleToken(Token.KwAnd, 3));
            else if (s == "or")
                tokens.Add(SimpleToken(Token.KwOr, 2));
            else if (s == "enum")
                tokens.Add(SimpleToken(Token.KwEnum, 4));
            else if (s == "case")
                tokens.Add(SimpleToken(Token.KwCase, 4));
            else
                tokens.Add(SimpleToken(Token.Sym, len));
        }
    }
}
