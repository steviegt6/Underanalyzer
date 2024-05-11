﻿using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Underanalyzer.Decompiler.AST;
using Underanalyzer.Mock;

namespace Underanalyzer.Decompiler.ControlFlow;

/// <summary>
/// Represents a basic block of VM instructions.
/// </summary>
internal class Block : IControlFlowNode
{
    public int StartAddress { get; private set; }

    public int EndAddress { get; private set; }

    public List<IControlFlowNode> Predecessors { get; } = new();

    public List<IControlFlowNode> Successors { get; } = new();

    public List<IGMInstruction> Instructions { get; }

    public IControlFlowNode Parent { get; set; } = null;

    public List<IControlFlowNode> Children => null;

    public bool Unreachable { get; set; } = false;

    public int BlockIndex { get; }

    public Block(int startAddr, int endAddr, int blockIndex, List<IGMInstruction> instructions)
    {
        StartAddress = startAddr;
        EndAddress = endAddr;
        BlockIndex = blockIndex;
        Instructions = instructions;
    }

    /// <summary>
    /// Calculates addresses of basic VM blocks; located at instructions that are jumped to.
    /// </summary>
    private static HashSet<int> FindBlockAddresses(IGMCode code)
    {
        HashSet<int> addresses = [0, code.Length];

        for (int i = 0; i < code.InstructionCount; i++)
        {
            IGMInstruction instr = code.GetInstruction(i);
            switch (instr.Kind)
            {
                case IGMInstruction.Opcode.Branch:
                case IGMInstruction.Opcode.BranchTrue:
                case IGMInstruction.Opcode.BranchFalse:
                case IGMInstruction.Opcode.PushWithContext:
                    addresses.Add(instr.Address + 4);
                    addresses.Add(instr.Address + instr.BranchOffset);
                    break;
                case IGMInstruction.Opcode.PopWithContext:
                    if (!instr.PopWithContextExit)
                    {
                        addresses.Add(instr.Address + 4);
                        addresses.Add(instr.Address + instr.BranchOffset);
                    }
                    break;
                case IGMInstruction.Opcode.Exit:
                case IGMInstruction.Opcode.Return:
                    addresses.Add(instr.Address + 4);
                    break;
                case IGMInstruction.Opcode.Call:
                    // Handle try hook addresses
                    if (i >= 4 && instr.Function.Name?.Content == VMConstants.TryHookFunction)
                    {
                        // If too close to end, bail
                        if (i >= code.InstructionCount - 1)
                            break;

                        // Check instructions
                        IGMInstruction finallyInstr = code.GetInstruction(i - 4);
                        IGMInstruction catchInstr = code.GetInstruction(i - 2);
                        IGMInstruction popInstr = code.GetInstruction(i + 1);
                        if (finallyInstr is not { Kind: IGMInstruction.Opcode.Push, Type1: IGMInstruction.DataType.Int32 } ||
                            catchInstr is not { Kind: IGMInstruction.Opcode.Push, Type1: IGMInstruction.DataType.Int32 } ||
                            popInstr is not { Kind: IGMInstruction.Opcode.PopDelete })
                        {
                            throw new DecompilerException("Expected Push with type Int32 before try hook");
                        }

                        // Add connections to referenced blocks
                        int finallyBlock = finallyInstr.ValueInt;
                        addresses.Add(finallyBlock);

                        int catchBlock = catchInstr.ValueInt;
                        if (catchBlock != -1)
                            addresses.Add(catchBlock);

                        // Split this try hook into its own block - removes edge cases in later graph operations
                        addresses.Add(finallyInstr.Address);
                        addresses.Add(popInstr.Address + IGMInstruction.GetSize(popInstr));
                    }
                    break;
            }
        }

        return addresses;
    }

    /// <summary>
    /// Finds all blocks from a given code entry, generating a basic control flow graph.
    /// </summary>
    public static List<Block> FindBlocks(DecompileContext ctx)
    {
        IGMCode code = ctx.Code;
        HashSet<int> addresses = FindBlockAddresses(code);

        Dictionary<int, Block> blocksByAddress = new();
        List<Block> blocks = new();
        Block current = null;
        for (int i = 0; i < code.InstructionCount; i++)
        {
            // Check if we have a new block at the current instruction's address
            IGMInstruction instr = code.GetInstruction(i);
            if (addresses.Contains(instr.Address))
            {
                // End previous block
                if (current != null)
                {
                    current.EndAddress = instr.Address;
                }

                // Make new block
                current = new(instr.Address, -1, blocks.Count, []);
                blocks.Add(current);
                blocksByAddress[current.StartAddress] = current;
            }

            // Add current instruction to our currently-building block
            current.Instructions.Add(instr);
        }

        // End current block, if applicable
        if (current != null)
        {
            current.EndAddress = code.Length;
        }

        // Add ending block
        Block end = new(code.Length, code.Length, blocks.Count, []);
        blocks.Add(end);
        blocksByAddress[end.StartAddress] = end;

        // Connect blocks together to construct flow graph
        foreach (Block b in blocks)
        {
            if (b.StartAddress == code.Length)
            {
                continue;
            }

            IGMInstruction last = b.Instructions[^1];
            switch (last.Kind)
            {
                case IGMInstruction.Opcode.Branch:
                    {
                        // Connect to block at destination address
                        Block dest = blocksByAddress[last.Address + last.BranchOffset];
                        b.Successors.Add(dest);
                        dest.Predecessors.Add(b);
                    }
                    break;
                case IGMInstruction.Opcode.BranchTrue:
                case IGMInstruction.Opcode.BranchFalse:
                case IGMInstruction.Opcode.PushWithContext:
                    {
                        // Connect to block directly after this current one, first
                        Block next = blocksByAddress[b.EndAddress];
                        b.Successors.Add(next);
                        next.Predecessors.Add(b);

                        // Connect to block at destination address, second
                        Block dest = blocksByAddress[last.Address + last.BranchOffset];
                        b.Successors.Add(dest);
                        dest.Predecessors.Add(b);
                    }
                    break;
                case IGMInstruction.Opcode.PopWithContext:
                    if (!last.PopWithContextExit)
                    {
                        // Connect to block directly after this current one, first
                        Block next = blocksByAddress[b.EndAddress];
                        b.Successors.Add(next);
                        next.Predecessors.Add(b);

                        // Connect to block at destination address, second
                        Block dest = blocksByAddress[last.Address + last.BranchOffset];
                        b.Successors.Add(dest);
                        dest.Predecessors.Add(b);
                    }
                    else
                    {
                        // Connect to block directly after this current one, only
                        Block next = blocksByAddress[b.EndAddress];
                        b.Successors.Add(next);
                        next.Predecessors.Add(b);
                    }
                    break;
                case IGMInstruction.Opcode.PopDelete:
                    {
                        // First, connect to block directly after this current one
                        Block next = blocksByAddress[b.EndAddress];
                        b.Successors.Add(next);
                        next.Predecessors.Add(b);

                        // Check for a block that was (theoretically) split earlier to only contain a try hook
                        if (b.Instructions.Count == 6)
                        {
                            IGMInstruction callInstr = b.Instructions[^2];
                            if (callInstr.Kind == IGMInstruction.Opcode.Call &&
                                callInstr.Function.Name?.Content == VMConstants.TryHookFunction)
                            {
                                // We've found a try hook - connect to targets
                                int finallyAddr = b.Instructions[^6].ValueInt;
                                int catchAddr = b.Instructions[^4].ValueInt;

                                // Add finally/end block target
                                Block finallyBlock = blocksByAddress[finallyAddr];
                                b.Successors.Add(finallyBlock);
                                finallyBlock.Predecessors.Add(b);

                                // If -1, we don't have a catch block at all
                                if (catchAddr != -1)
                                {
                                    Block catchBlock = blocksByAddress[catchAddr];
                                    b.Successors.Add(catchBlock);
                                    catchBlock.Predecessors.Add(b);
                                }
                            }
                        }
                    }
                    break;
                case IGMInstruction.Opcode.Exit:
                case IGMInstruction.Opcode.Return:
                    // Do nothing - code execution terminates here
                    break;
                default:
                    {
                        // Connect to block directly after this current one
                        Block next = blocksByAddress[b.EndAddress];
                        b.Successors.Add(next);
                        next.Predecessors.Add(b);
                    }
                    break;
            }
        }

        // Compute blocks that are unreachable
        for (int i = 1; i < blocks.Count; i++)
        {
            if (blocks[i].Predecessors.Count == 0)
            {
                blocks[i].Unreachable = true;
                blocks[i].Predecessors.Add(blocks[i - 1]);
                blocks[i - 1].Successors.Add(blocks[i]);
            }
        }

        ctx.Blocks = blocks;
        return blocks;
    }

    public override string ToString()
    {
        return $"{nameof(Block)} {BlockIndex} ({Instructions.Count} instructions, {Predecessors.Count} predecessors, {Successors.Count} successors)";
    }

    public void BuildAST(ASTBuilder builder, List<IStatementNode> output)
    {
        BlockSimulator.Simulate(builder, output, this);
    }
}
