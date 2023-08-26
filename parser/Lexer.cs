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
        // -
        Minus,
        // ->
        Arrow,
        // the 'let' keyword
        KwLet,
        // the 'def' keyword
        KwDef,
        // the 'return' keyword
        KwReturn,
    }

    public class Lexer
    {
        private string Source;
        private int Pos = 0;
        private List<Superpower.Model.Token<Token>> tokens = new List<Superpower.Model.Token<Token>>();
        private int currentIndent = 0;

        public static List<Superpower.Model.Token<Token>> TokenizeString(string s)
        {
            Lexer l = new Lexer { Source = s };
            l.LexAll();
            return l.tokens;
        }

        private Superpower.Model.TextSpan Span(int length)
        {
            // TODO: line and column
            return new Superpower.Model.TextSpan(Source, new Superpower.Model.Position(Pos, 0, 0), length);
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
                    tokens.Add(SimpleToken(Token.Assign, 1));
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
                else if (ch == '-')
                {
                    if (Pos + 1 < Source.Length && Source[Pos + 1] == '>')
                        tokens.Add(SimpleToken(Token.Arrow, 2));
                    else
                        tokens.Add(SimpleToken(Token.Minus, 1));
                }
                // symbols and numbers
                else if (char.IsDigit(ch))
                    LexInt();
                else if (char.IsLetter(ch))
                    LexSymbol();
                else
                {
                    tokens.Add(SimpleToken(Token.Error, 1));
                    return;
                }

            }
            // at the end, before EOI, emit a sufficient amount of dedents
            while (currentIndent > 0)
            {
                tokens.Add(new Superpower.Model.Token<Token>(Token.Dedent, Span(0)));
                currentIndent--;
            }
            tokens.Add(new Superpower.Model.Token<Token>(Token.Eoi, Span(0)));
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
            while ((Pos + len) < Source.Length && char.IsLetterOrDigit(Source[Pos + len]))
                len++;
            string s = Source.Substring(Pos, len);
            if (s == "let")
                tokens.Add(SimpleToken(Token.KwLet, 3));
            else if (s == "def")
                tokens.Add(SimpleToken(Token.KwDef, 3));
            else if (s == "return")
                tokens.Add(SimpleToken(Token.KwReturn, 6));
            else
                tokens.Add(SimpleToken(Token.Sym, len));
        }
    }
}
