﻿using Underanalyzer;
using Underanalyzer.Decompiler;
using Underanalyzer.Mock;
using static System.Reflection.Metadata.BlobBuilder;

namespace UnderanalyzerTest;

public class Loop_FindLoops
{
    [Fact]
    public void TestNone()
    {
        GMCode code = TestUtil.GetCode(
            """
            pushi.e 123
            """
        );
        List<Block> blocks = Block.FindBlocks(code);
        List<Fragment> fragments = Fragment.FindFragments(code, blocks);
        List<Loop> loops = Loop.FindLoops(blocks);

        Assert.Empty(loops);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
    }

    [Fact]
    public void TestSingleWhile()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.i
            pushi.e 10
            cmp.i.v LT
            bf [end]

            :[1]
            push.v self.i
            push.e 1
            add.i.v
            pop.v.v self.i
            b [0]

            :[end]
            """
        );
        List<Block> blocks = Block.FindBlocks(code);
        List<Fragment> fragments = Fragment.FindFragments(code, blocks);
        List<Loop> loops = Loop.FindLoops(blocks);

        Assert.Single(loops);
        Assert.IsType<WhileLoop>(loops[0]);
        WhileLoop loop0 = (WhileLoop)loops[0];
        Assert.Equal(loop0, fragments[0].Children[0]);
        Assert.Equal(blocks[0], loop0.Head);
        Assert.Equal(blocks[1], loop0.Tail);
        Assert.NotEqual(blocks[2], loop0.After);
        Assert.Empty(loop0.After.Successors);
        Assert.Equal(blocks[2].Predecessors[0], loop0);
        Assert.Empty(loop0.Predecessors);
        Assert.Equal(fragments[0], loop0.Parent);
        Assert.Equal(blocks[1], loop0.Body);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
    }

    [Fact]
    public void TestNestedWhile()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            pushi.e 0
            pop.v.i self.i

            :[1]
            push.v self.i
            pushi.e 10
            cmp.i.v LT
            bf [end]

            :[2]
            push.v self.j
            pushi.e 10
            cmp.i.v LT
            bf [4]

            :[3]
            push.v self.j
            push.e 1
            add.i.v
            pop.v.v self.j
            b [2]

            :[4]
            push.v self.i
            push.e 1
            add.i.v
            pop.v.v self.i
            b [1]

            :[end]
            """
        );
        List<Block> blocks = Block.FindBlocks(code);
        List<Fragment> fragments = Fragment.FindFragments(code, blocks);
        List<Loop> loops = Loop.FindLoops(blocks);

        Assert.Equal(2, loops.Count);
        Assert.IsType<WhileLoop>(loops[0]);
        Assert.IsType<WhileLoop>(loops[1]);
        WhileLoop loop0 = (WhileLoop)loops[0];
        WhileLoop loop1 = (WhileLoop)loops[1];
        Assert.Equal(loop0, fragments[0].Children[0].Successors[0]);
        Assert.Equal(loop1, loop0.Body);
        Assert.Equal(loop0, loop1.Parent);
        Assert.Equal(blocks[3], loop1.Body);
        Assert.Equal(blocks[1], loop0.Head);
        Assert.Equal(blocks[2], loop1.Head);
        Assert.Empty(loop1.Predecessors);
        Assert.Equal(blocks[4], loop1.Successors[0]);
        Assert.Empty(blocks[4].Successors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
    }

    [Fact]
    public void TestSequentialWhile()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            pushi.e 0
            pop.v.i self.i

            :[1]
            push.v self.i
            pushi.e 10
            cmp.i.v LT
            bf [3]

            :[2]
            push.v self.i
            push.e 1
            add.i.v
            pop.v.v self.i
            b [1]

            :[3]
            push.v self.i
            pushi.e 20
            cmp.i.v LT
            bf [end]

            :[4]
            push.v self.i
            push.e 1
            add.i.v
            pop.v.v self.i
            b [3]

            :[end]
            """
        );
        List<Block> blocks = Block.FindBlocks(code);
        List<Fragment> fragments = Fragment.FindFragments(code, blocks);
        List<Loop> loops = Loop.FindLoops(blocks);

        Assert.Equal(2, loops.Count);
        Assert.IsType<WhileLoop>(loops[0]);
        Assert.IsType<WhileLoop>(loops[1]);
        WhileLoop loop0 = (WhileLoop)loops[0];
        WhileLoop loop1 = (WhileLoop)loops[1];
        Assert.Equal(loop0, blocks[0].Successors[0]);
        Assert.Equal(blocks[2], loop0.Body);
        Assert.Equal(loop1, loop0.Successors[0]);
        Assert.Equal(loop0, loop1.Predecessors[0]);
        Assert.Empty(blocks[3].Predecessors);
        Assert.Equal(blocks[2], loop0.Tail);
        Assert.Equal(blocks[4], loop1.Tail);
        Assert.Empty(loop0.Tail.Successors);
        Assert.Empty(loop1.Tail.Successors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
    }

    [Fact]
    public void TestSingleDoUntil()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.a
            push.e 1
            add.i.v
            pop.v.v self.a
            push.v self.a
            pushi.e 10
            cmp.i.v GTE
            bf [0]
            """
        );
        List<Block> blocks = Block.FindBlocks(code);
        List<Fragment> fragments = Fragment.FindFragments(code, blocks);
        List<Loop> loops = Loop.FindLoops(blocks);

        Assert.Single(loops);
        Assert.IsType<DoUntilLoop>(loops[0]);
        DoUntilLoop loop0 = (DoUntilLoop)loops[0];
        Assert.Equal(fragments[0], loop0.Parent);
        Assert.Equal(loop0, fragments[0].Children[0]);
        Assert.Equal(blocks[0], loop0.Head);
        Assert.Equal(blocks[0], loop0.Tail);
        Assert.IsType<EmptyNode>(loop0.After);
        Assert.Empty(loop0.Predecessors);
        Assert.Equal(blocks[1], loop0.Successors[0]);
        Assert.Empty(blocks[0].Predecessors);
        Assert.Empty(blocks[0].Successors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
    }

    [Fact]
    public void TestNestedDoUntil()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.b
            push.e 1
            add.i.v
            pop.v.v self.b
            push.v self.b
            pushi.e 10
            cmp.i.v GTE
            bf [0]

            :[1]
            push.v self.a
            pushi.e 10
            cmp.i.v GTE
            bf [0]
            """
        );
        List<Block> blocks = Block.FindBlocks(code);
        List<Fragment> fragments = Fragment.FindFragments(code, blocks);
        List<Loop> loops = Loop.FindLoops(blocks);

        Assert.Equal(2, loops.Count);
        Assert.IsType<DoUntilLoop>(loops[0]);
        Assert.IsType<DoUntilLoop>(loops[1]);
        DoUntilLoop loop0 = (DoUntilLoop)loops[0];
        DoUntilLoop loop1 = (DoUntilLoop)loops[1];
        Assert.Equal(fragments[0], loop0.Parent);
        Assert.Equal(loop0, loop1.Parent);
        Assert.Equal(blocks[0], loop1.Head);
        Assert.Equal(blocks[0], loop1.Tail);
        Assert.IsType<EmptyNode>(loop1.After);
        Assert.Empty(loop1.Predecessors);
        Assert.Equal(blocks[1], loop1.Successors[0]);
        Assert.Empty(blocks[1].Successors);
        Assert.Equal(loop1, loop0.Head);
        Assert.Equal(blocks[1], loop0.Tail);
        Assert.IsType<EmptyNode>(loop0.After);
        Assert.Empty(loop0.Predecessors);
        Assert.Equal(blocks[2], loop0.Successors[0]);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
    }

    [Fact]
    public void TestNestedDoUntil2()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.a
            push.e 1
            add.i.v
            pop.v.v self.a

            :[1]
            push.v self.b
            push.e 1
            add.i.v
            pop.v.v self.b
            push.v self.b
            pushi.e 10
            cmp.i.v GTE
            bf [1]

            :[2]
            push.v self.a
            pushi.e 10
            cmp.i.v GTE
            bf [0]
            """
        );
        List<Block> blocks = Block.FindBlocks(code);
        List<Fragment> fragments = Fragment.FindFragments(code, blocks);
        List<Loop> loops = Loop.FindLoops(blocks);

        Assert.Equal(2, loops.Count);
        Assert.IsType<DoUntilLoop>(loops[0]);
        Assert.IsType<DoUntilLoop>(loops[1]);
        DoUntilLoop loop0 = (DoUntilLoop)loops[0];
        DoUntilLoop loop1 = (DoUntilLoop)loops[1];
        Assert.Equal(fragments[0], loop0.Parent);
        Assert.Equal(loop1, blocks[1].Parent);
        Assert.Equal(blocks[1], loop1.Head);
        Assert.Equal(blocks[1], loop1.Tail);
        Assert.IsType<EmptyNode>(loop1.After);
        Assert.Equal([blocks[0]], loop1.Predecessors);
        Assert.Equal(blocks[2], loop1.Successors[0]);
        Assert.Empty(blocks[2].Successors);
        Assert.Equal(blocks[0], loop0.Head);
        Assert.Equal(blocks[2], loop0.Tail);
        Assert.IsType<EmptyNode>(loop0.After);
        Assert.Empty(loop0.Predecessors);
        Assert.Equal(blocks[3], loop0.Successors[0]);
        Assert.Equal(loop0, blocks[0].Parent);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
    }

    [Fact]
    public void TestSequentialDoUntil()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.a
            pushi.e 10
            cmp.i.v GTE
            bf [0]

            :[1]
            push.v self.b
            pushi.e 10
            cmp.i.v GTE
            bf [1]
            """
        );
        List<Block> blocks = Block.FindBlocks(code);
        List<Fragment> fragments = Fragment.FindFragments(code, blocks);
        List<Loop> loops = Loop.FindLoops(blocks);

        Assert.Equal(2, loops.Count);
        Assert.IsType<DoUntilLoop>(loops[0]);
        Assert.IsType<DoUntilLoop>(loops[1]);
        DoUntilLoop loop0 = (DoUntilLoop)loops[0];
        DoUntilLoop loop1 = (DoUntilLoop)loops[1];
        Assert.Empty(loop0.Predecessors);
        Assert.Equal([loop1], loop0.Successors);
        Assert.Equal([blocks[2]], loop1.Successors);
        Assert.Equal(blocks[0], loop0.Head);
        Assert.Equal(blocks[0], loop0.Tail);
        Assert.IsType<EmptyNode>(loop0.After);
        Assert.Equal(blocks[1], loop1.Head);
        Assert.Equal(blocks[1], loop1.Tail);
        Assert.IsType<EmptyNode>(loop1.After);
        Assert.Empty(loop0.Head.Predecessors);
        Assert.Empty(loop0.Tail.Successors);
        Assert.Empty(loop1.Head.Predecessors);
        Assert.Empty(loop1.Tail.Successors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
    }

    [Fact]
    public void TestWhileDoUntil()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.a
            pushi.e 10
            cmp.i.v LT
            bf [3]

            :[1]
            push.v self.b
            pushi.e 10
            cmp.i.v GTE
            bf [1]

            :[2]
            b [0]

            :[3]
            """
        );
        List<Block> blocks = Block.FindBlocks(code);
        List<Fragment> fragments = Fragment.FindFragments(code, blocks);
        List<Loop> loops = Loop.FindLoops(blocks);

        Assert.Equal(2, loops.Count);
        Assert.IsType<WhileLoop>(loops[0]);
        Assert.IsType<DoUntilLoop>(loops[1]);
        WhileLoop loop0 = (WhileLoop)loops[0];
        DoUntilLoop loop1 = (DoUntilLoop)loops[1];
        Assert.Equal(fragments[0], loop0.Parent);
        Assert.Equal(loop0, loop1.Parent);
        Assert.Empty(loop1.Predecessors);
        Assert.Equal([blocks[2]], loop1.Successors);
        Assert.Empty(loop0.Predecessors);
        Assert.Equal([blocks[3]], loop0.Successors);
        Assert.Equal(blocks[1], loop1.Head);
        Assert.Equal(blocks[1], loop1.Tail);
        Assert.IsType<EmptyNode>(loop1.After);
        Assert.Equal(blocks[0], loop0.Head);
        Assert.Equal(loop1, loop0.Body);
        Assert.Equal(blocks[2], loop0.Tail);
        Assert.IsType<EmptyNode>(loop0.After);
        Assert.Empty(loop1.Head.Predecessors);
        Assert.Empty(loop1.Tail.Successors);
        Assert.Empty(loop0.Head.Predecessors);
        Assert.Empty(loop0.Tail.Successors);
        Assert.Empty(loop0.Body.Predecessors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
    }

    [Fact]
    public void TestDoUntilWhile()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            push.v self.b
            pushi.e 10
            cmp.i.v LT
            bf [2]

            :[1]
            b [0]

            :[2]
            push.v self.a
            pushi.e 10
            cmp.i.v GTE
            bf [0]
            """
        );
        List<Block> blocks = Block.FindBlocks(code);
        List<Fragment> fragments = Fragment.FindFragments(code, blocks);
        List<Loop> loops = Loop.FindLoops(blocks);

        Assert.Equal(2, loops.Count);
        Assert.IsType<DoUntilLoop>(loops[0]);
        Assert.IsType<WhileLoop>(loops[1]);
        DoUntilLoop loop0 = (DoUntilLoop)loops[0];
        WhileLoop loop1 = (WhileLoop)loops[1];
        Assert.Equal(loop0, loop1.Parent);
        Assert.Equal(fragments[0], loop0.Parent);
        Assert.Empty(loop1.Predecessors);
        Assert.Equal([blocks[2]], loop1.Successors);
        Assert.Empty(loop0.Predecessors);
        Assert.Equal([blocks[3]], loop0.Successors);
        Assert.Equal(loop1, loop0.Head);
        Assert.Equal(blocks[2], loop0.Tail);
        Assert.IsType<EmptyNode>(loop0.After);
        Assert.Equal(blocks[0], loop1.Head);
        Assert.Equal(blocks[1], loop1.Body);
        Assert.Equal(blocks[1], loop1.Tail);
        Assert.IsType<EmptyNode>(loop1.After);
        Assert.Empty(loop1.Head.Predecessors);
        Assert.Empty(loop1.Body.Predecessors);
        Assert.Empty(loop1.Tail.Successors);
        Assert.Empty(loop0.Head.Predecessors);
        Assert.Empty(loop0.Tail.Successors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
    }

    [Fact]
    public void TestSingleRepeat()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            pushi.e 100
            dup.i 0
            push.i 0
            cmp.i.i LTE
            bt [2]

            :[1]
            push.i 1
            sub.i.i
            dup.i 0
            conv.i.b
            bt [1]

            :[2]
            popz.i
            """
        );
        List<Block> blocks = Block.FindBlocks(code);
        List<Fragment> fragments = Fragment.FindFragments(code, blocks);
        List<Loop> loops = Loop.FindLoops(blocks);

        Assert.Single(loops);
        Assert.IsType<RepeatLoop>(loops[0]);
        RepeatLoop loop0 = (RepeatLoop)loops[0];
        Assert.Equal([loop0], blocks[0].Successors);
        Assert.Equal([blocks[2]], loop0.Successors);
        Assert.Equal(blocks[1], loop0.Head);
        Assert.Equal(blocks[1], loop0.Tail);
        Assert.IsType<EmptyNode>(loop0.After);
        Assert.Single(blocks[0].Instructions);
        Assert.Equal(IGMInstruction.Opcode.PushImmediate, blocks[0].Instructions[0].Kind);
        Assert.Empty(blocks[1].Instructions);
        Assert.Empty(blocks[2].Instructions);
        Assert.Empty(loop0.Head.Predecessors);
        Assert.Empty(loop0.Tail.Successors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
    }

    [Fact]
    public void TestNestedRepeat()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            pushi.e 100
            dup.i 0
            push.i 0
            cmp.i.i LTE
            bt [4]

            :[1]
            pushi.e 200
            dup.i 0
            push.i 0
            cmp.i.i LTE
            bt [3]

            :[2]
            push.i 1
            sub.i.i
            dup.i 0
            conv.i.b
            bt [2]

            :[3]
            popz.i
            push.i 1
            sub.i.i
            dup.i 0
            conv.i.b
            bt [1]

            :[4]
            popz.i
            """
        );
        List<Block> blocks = Block.FindBlocks(code);
        List<Fragment> fragments = Fragment.FindFragments(code, blocks);
        List<Loop> loops = Loop.FindLoops(blocks);

        Assert.Equal(2, loops.Count);
        Assert.IsType<RepeatLoop>(loops[0]);
        Assert.IsType<RepeatLoop>(loops[1]);
        RepeatLoop loop0 = (RepeatLoop)loops[0];
        RepeatLoop loop1 = (RepeatLoop)loops[1];
        Assert.Equal([loop1], loop0.Head.Successors);
        Assert.Empty(loop0.Tail.Successors);
        Assert.Equal(blocks[1], loop0.Head);
        Assert.Equal(blocks[3], loop0.Tail);
        Assert.IsType<EmptyNode>(loop0.After);
        Assert.Equal(blocks[2], loop1.Head);
        Assert.Equal(blocks[2], loop1.Tail);
        Assert.IsType<EmptyNode>(loop1.After);
        Assert.Equal([blocks[1]], loop1.Predecessors);
        Assert.Equal([blocks[3]], loop1.Successors);
        Assert.Equal([blocks[0]], loop0.Predecessors);
        Assert.Equal([blocks[4]], loop0.Successors);
        Assert.Single(blocks[0].Instructions);
        Assert.Equal(IGMInstruction.Opcode.PushImmediate, blocks[0].Instructions[0].Kind);
        Assert.Single(blocks[1].Instructions);
        Assert.Equal(IGMInstruction.Opcode.PushImmediate, blocks[1].Instructions[0].Kind);
        Assert.Empty(blocks[2].Instructions);
        Assert.Empty(blocks[3].Instructions);
        Assert.Empty(blocks[4].Instructions);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
    }

    [Fact]
    public void TestSingleEmptyWith()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            pushi.e 123
            pushenv [1]

            :[1]
            popenv [1]
            """
        );
        List<Block> blocks = Block.FindBlocks(code);
        List<Fragment> fragments = Fragment.FindFragments(code, blocks);
        List<Loop> loops = Loop.FindLoops(blocks);

        Assert.Single(loops);
        Assert.IsType<WithLoop>(loops[0]);
        WithLoop loop0 = (WithLoop)loops[0];
        Assert.Equal(blocks[0], loop0.Predecessors[0]);
        Assert.Equal(blocks[1], loop0.Head);
        Assert.Equal(blocks[1], loop0.Tail);
        Assert.IsType<EmptyNode>(loop0.After);
        Assert.Null(loop0.BreakBlock);
        Assert.Equal(blocks[2], loop0.Successors[0]);
        Assert.Empty(loop0.Head.Predecessors);
        Assert.Empty(loop0.Tail.Successors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
    }

    [Fact]
    public void TestSingleWith()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            pushi.e 123
            pushenv [2]

            :[1]
            pushi.e 123
            pop.v.i self.a

            :[2]
            popenv [1]
            """
        );
        List<Block> blocks = Block.FindBlocks(code);
        List<Fragment> fragments = Fragment.FindFragments(code, blocks);
        List<Loop> loops = Loop.FindLoops(blocks);

        Assert.Single(loops);
        Assert.IsType<WithLoop>(loops[0]);
        WithLoop loop0 = (WithLoop)loops[0];
        Assert.Equal(blocks[0], loop0.Predecessors[0]);
        Assert.Equal(blocks[1], loop0.Head);
        Assert.Equal(blocks[2], loop0.Tail);
        Assert.IsType<EmptyNode>(loop0.After);
        Assert.Null(loop0.BreakBlock);
        Assert.Equal(blocks[3], loop0.Successors[0]);
        Assert.Empty(loop0.Head.Predecessors);
        Assert.Empty(loop0.Tail.Successors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
    }

    [Fact]
    public void TestSingleBreakWith()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            pushi.e 123
            pushenv [3]

            :[1]
            push.v self.a
            conv.v.b
            bf [3]

            :[2]
            b [5]

            :[3]
            popenv [1]

            :[4]
            b [6]

            :[5]
            popenv <drop>

            :[6]
            """
        );
        List<Block> blocks = Block.FindBlocks(code);
        List<Fragment> fragments = Fragment.FindFragments(code, blocks);
        List<Loop> loops = Loop.FindLoops(blocks);

        Assert.Single(loops);
        Assert.IsType<WithLoop>(loops[0]);
        WithLoop loop0 = (WithLoop)loops[0];
        Assert.Equal(blocks[0], loop0.Predecessors[0]);
        Assert.Equal(blocks[1], loop0.Head);
        Assert.Equal(blocks[3], loop0.Tail);
        Assert.IsType<EmptyNode>(loop0.After);
        Assert.Equal(blocks[5], loop0.BreakBlock);
        Assert.Equal(blocks[6], loop0.Successors[0]);
        Assert.Empty(loop0.Head.Predecessors);
        Assert.Empty(loop0.Tail.Successors);
        Assert.Empty(loop0.BreakBlock.Predecessors);
        Assert.Empty(loop0.BreakBlock.Successors);
        Assert.Equal([loop0.After], blocks[2].Successors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
    }

    [Fact]
    public void TestNestedWith()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            pushi.e 123
            pushenv [4]

            :[1]
            pushi.e 456
            pushenv [3]

            :[2]
            pushi.e 789
            pop.v.i self.a

            :[3]
            popenv [2]

            :[4]
            popenv [1]

            :[5]
            """
        );
        List<Block> blocks = Block.FindBlocks(code);
        List<Fragment> fragments = Fragment.FindFragments(code, blocks);
        List<Loop> loops = Loop.FindLoops(blocks);

        Assert.Equal(2, loops.Count);
        Assert.IsType<WithLoop>(loops[0]);
        Assert.IsType<WithLoop>(loops[1]);
        WithLoop loop0 = (WithLoop)loops[0];
        WithLoop loop1 = (WithLoop)loops[1];
        Assert.Equal([blocks[0]], loop0.Predecessors);
        Assert.Equal([blocks[5]], loop0.Successors);
        Assert.Equal([blocks[1]], loop1.Predecessors);
        Assert.Equal(blocks[1], loop0.Head);
        Assert.Equal(blocks[4], loop0.Tail);
        Assert.IsType<EmptyNode>(loop0.After);
        Assert.Null(loop0.BreakBlock);
        Assert.Equal(blocks[2], loop1.Head);
        Assert.Equal(blocks[3], loop1.Tail);
        Assert.IsType<EmptyNode>(loop1.After);
        Assert.Null(loop1.BreakBlock);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
    }

    [Fact]
    public void TestNestedBreakWith()
    {
        GMCode code = TestUtil.GetCode(
            """
            :[0]
            pushi.e 123
            pushenv [7]

            :[1]
            pushi.e 456
            pushenv [3]

            :[2]
            pushi.e 789
            pop.v.i self.a
            b [5]

            :[3]
            popenv [2]

            :[4]
            b [6]

            :[5]
            popenv <drop>

            :[6]
            b [8]

            :[7]
            popenv [1]

            :[8]
            b [10]

            :[9]
            popenv <drop>

            :[10]
            """
        );
        List<Block> blocks = Block.FindBlocks(code);
        List<Fragment> fragments = Fragment.FindFragments(code, blocks);
        List<Loop> loops = Loop.FindLoops(blocks);

        Assert.Equal(2, loops.Count);
        Assert.IsType<WithLoop>(loops[0]);
        Assert.IsType<WithLoop>(loops[1]);
        WithLoop loop0 = (WithLoop)loops[0];
        WithLoop loop1 = (WithLoop)loops[1];
        Assert.Equal([blocks[0]], loop0.Predecessors);
        Assert.Equal([blocks[10]], loop0.Successors);
        Assert.Equal([blocks[1]], loop1.Predecessors);
        Assert.Equal(blocks[1], loop0.Head);
        Assert.Equal(blocks[7], loop0.Tail);
        Assert.IsType<EmptyNode>(loop0.After);
        Assert.Equal(blocks[9], loop0.BreakBlock);
        Assert.Empty(loop0.BreakBlock.Predecessors);
        Assert.Empty(loop0.BreakBlock.Successors);
        Assert.Equal(blocks[2], loop1.Head);
        Assert.Equal(blocks[3], loop1.Tail);
        Assert.IsType<EmptyNode>(loop1.After);
        Assert.Equal(blocks[5], loop1.BreakBlock);
        Assert.Empty(loop1.BreakBlock.Predecessors);
        Assert.Empty(loop1.BreakBlock.Successors);
        Assert.Equal([loop1.After], blocks[2].Successors);
        Assert.Equal([loop0.After], blocks[6].Successors);

        TestUtil.VerifyFlowDirections(blocks);
        TestUtil.VerifyFlowDirections(fragments);
        TestUtil.VerifyFlowDirections(loops);
    }
}