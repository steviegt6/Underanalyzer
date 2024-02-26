﻿using System;
using System.Collections.Generic;

namespace Underanalyzer.Mock;

public class GMCode : IGMCode
{
    public GMString Name { get; set; }
    public List<GMInstruction> Instructions { get; set; }
    public GMCode Parent { get; set; } = null;
    public List<GMCode> Children { get; set; } = new();
    public uint StartOffset { get; set; } = 0;
    public int ArgumentCount { get; set; } = 1;
    public int LocalCount { get; set; } = 0;

    public GMCode(string name, List<GMInstruction> instructions)
    {
        Name = new(name);
        Instructions = instructions;
    }

    // Interface implementation
    IGMString IGMCode.Name => Name;
    public int InstructionCount => Instructions.Count;
    IGMCode IGMCode.Parent => Parent;
    public int ChildCount => Children.Count;

    public IGMCode GetChild(int index) => Children[index];
    public IGMInstruction GetInstruction(int index) => Instructions[index];
}

public class GMInstruction : IGMInstruction
{
    public uint Address { get; set; }
    public IGMInstruction.Opcode Kind { get; set; }
    public IGMInstruction.ExtendedOpcode ExtKind { get; set; }
    public IGMInstruction.ComparisonType ComparisonKind { get; set; }
    public IGMInstruction.DataType Type1 { get; set; }
    public IGMInstruction.DataType Type2 { get; set; }
    public IGMInstruction.InstanceType InstType { get; set; }
    public IGMVariable Variable { get; set; }
    public IGMFunction Function { get; set; }
    public IGMInstruction.VariableType ReferenceVarType { get; set; }
    public double ValueDouble { get; set; }
    public short ValueShort { get; set; }
    public int ValueInt { get; set; }
    public long ValueLong { get; set; }
    public bool ValueBool { get; set; }
    public IGMString ValueString { get; set; }
    public int BranchOffset { get => ValueInt; set => ValueInt = value; }
    public bool PopWithContextExit { get => ValueBool; set => ValueBool = value; }
    public byte DuplicationSize { get; set; }
    public byte DuplicationSize2 { get; set; }
    public int ArgumentCount { get => ValueInt; set => ValueInt = value; }
    public int PopSwapSize { get => ValueInt; set => ValueInt = value; }
}

public class GMString : IGMString
{
    public string Content { get; set; }

    public GMString(string content) => Content = content;
}

public class GMVariable : IGMVariable
{
    public IGMString Name { get; set; }

    public IGMInstruction.InstanceType InstanceType { get; set; }

    public int VariableID { get; set; }
}

public class GMFunction : IGMFunction
{
    public IGMString Name { get; set; }

    public GMFunction(string name) => Name = new GMString(name);
}