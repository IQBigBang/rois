﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.ast
{
    public abstract record Expr()
    {
        public abstract bool IsValidLhs();
    };

    public record IntExpr(int Value) : Expr()
    {
        public override bool IsValidLhs() => false;
    }

    public record VarExpr(string Name) : Expr()
    {
        public override bool IsValidLhs() => true;
    }

    public record BinOpExpr(Expr Lhs, Expr Rhs, BinOpExpr.Ops Op) : Expr()
    {
        public enum Ops
        {
            Add
        }
        public override bool IsValidLhs() => false;
    }
}
