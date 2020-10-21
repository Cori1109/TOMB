﻿using Phantasma.Domain;
using System;
using System.Collections.Generic;

namespace Phantasma.Tomb.Compiler
{
    public class Script: Module
    {
        public StatementBlock main;

        public MethodParameter[] Parameters { get; internal set; }
        public VarKind ReturnType;

        public Script(string name, bool hidden) : base(name, hidden)
        {

        }

        public override bool IsNodeUsed(Node node)
        {
            if (node == this)
            {
                return true;
            }

            foreach (var lib in Libraries.Values)
            {
                if (lib.IsNodeUsed(node))
                {
                    return true;
                }
            }


            return main.IsNodeUsed(node);
        }

        public override void Visit(Action<Node> callback)
        {
            foreach (var lib in Libraries.Values)
            {
                lib.Visit(callback);
            }

            callback(this);
            main.Visit(callback);
        }


        public override ContractInterface GenerateCode(CodeGenerator output)
        {
            this.Scope.Enter(output);

            this.main.ParentScope.Enter(output);

            foreach (var parameter in this.Parameters)
            {
                var reg = Parser.Instance.AllocRegister(output, this, parameter.Name);
                output.AppendLine(this, $"POP {reg}");

                if (!this.main.ParentScope.Variables.ContainsKey(parameter.Name))
                {
                    throw new CompilerException("script parameter not initialized: " + parameter.Name);
                }

                var varDecl = this.main.ParentScope.Variables[parameter.Name];
                varDecl.Register = reg;
            }

            this.main.GenerateCode(output);
            this.main.ParentScope.Leave(output);

            if (ReturnType == VarKind.None)
            {
                output.AppendLine(this, "RET");
            }
            else
            {
                bool hasReturn = false;
                this.main.Visit((node) =>
                {
                    if (node is ReturnStatement)
                    {
                        hasReturn = true;
                    }
                });

                if (!hasReturn)
                {
                    throw new Exception("Script is missing return statement");
                }
            }

            this.Scope.Leave(output);

            return null;
            //return new ContractInterface(Enumerable.Empty<ContractMethod>(), Enumerable.Empty<ContractEvent>());
        }

    }
}
