using QuikGraph;
using QuikGraph.Graphviz.Dot;
using RoisLang.mid_ir;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoisLang.asm
{
    /// <summary>
    /// This is a class which implements the algorithm
    /// of moving and swapping values across registers.
    /// 
    /// This is not as simple as it sounds. Consider a set of moves:
    /// ```
    /// 3 -> R1
    /// R1 -> R2
    /// ```
    /// 
    /// Compiling this naively to `mov r1, 3; mov r2, r1` results in a miscompilation.
    /// 
    /// We use the `QuikGraph` library to implement a graph-based algorithm
    /// </summary>
    internal class MovesAlgorithm
    {
        public static List<MoveInstr> CompileMoves(MidValue[] values, GpReg[] dest, bool reverse, Dictionary<MidValue, GpReg> allocatedRegs)
        {
            var graph = ConstructGraph(values, dest, reverse, allocatedRegs);

            List<MoveInstr> instructions = new();
            while (graph.VertexCount > 0)
            {
                /*var renderAlg = new QuikGraph.Graphviz.GraphvizAlgorithm<Vertex, Edge<Vertex>>(graph);
                renderAlg.FormatVertex += RenderAlg_FormatVertex;
                Console.WriteLine(renderAlg.Generate());*/

                // the algorithm works as follows:
                // find a register which has no output edges (if not found => perform decycling)
                // walk backwards from it while emitting `mov`s
                foreach (var vert in graph.Vertices)
                {
                    if (graph.IsOutEdgesEmpty(vert))
                    {
                        // We found our starting node
                        //Console.WriteLine("Starting at " + vert);
                        var inputs = GetInputEdgesOf(graph, vert);
                        if (inputs.Count == 0)
                        {
                            // this vertex has neither input, nor output edges -> remove it
                            graph.RemoveVertex(vert);
                            goto next; 
                        }
                        if (inputs.Count > 1) throw new ArgumentException("Invalid graph"); // the graph is invalid
                        var inpEdge = inputs[0];
                        // emit the MovInstr
                        var destReg = (VertRegister)inpEdge.Target;
                        if (inpEdge.Source is VertRegister vr)
                            instructions.Add(new MovRR(vr.gpReg, destReg.gpReg));
                        if (inpEdge.Source is VertValue vv)
                            instructions.Add(new MovIR(vv.iValue, destReg.gpReg));
                        // remove the edge
                        graph.RemoveEdge(inpEdge);
                        // remove the vertex if it has no other connections
                        if (graph.IsOutEdgesEmpty(vert))
                            graph.RemoveVertex(vert);
                        goto next;
                    }
                }
                // if we reached this point, there are still nodes but all of them
                // have an output => a cycle exists
                if (TryResolveCycle(graph, instructions)) goto next;
                // right before failing, print the graph for debug reasons
                var renderAlg = new QuikGraph.Graphviz.GraphvizAlgorithm<Vertex, Edge<Vertex>>(graph);
                renderAlg.FormatVertex += RenderAlg_FormatVertex;
                Console.WriteLine(renderAlg.Generate());
                throw new NotImplementedException("cycles not supported yet");
            
                next: continue;
            }

            return instructions;

        }

        private static bool TryResolveCycle(AdjacencyGraph<Vertex, Edge<Vertex>> graph, List<MoveInstr> outInstrs)
        {
            // We handle the very simple case of a two-cycle
            if (graph.VertexCount != 2) return false;
            var vertA = graph.Vertices.ElementAt(0);
            var vertB = graph.Vertices.ElementAt(1);
            if (graph.OutEdges(vertA).All(e => e.Target == vertB) && graph.OutEdges(vertB).All(e => e.Target == vertA))
            {
                outInstrs.Add(new Swap(((VertRegister)vertA).gpReg, ((VertRegister)vertB).gpReg));
                graph.RemoveVertex(vertA);
                graph.RemoveVertex(vertB);
                return true;
            }
            else return false;
        }

        private static List<Edge<Vertex>> GetInputEdgesOf(AdjacencyGraph<Vertex, Edge<Vertex>> graph, Vertex outVertex)
        {
            List<Edge<Vertex>> list = new();
            foreach (var vert in graph.Vertices)
            {
                if (graph.TryGetEdge(vert, outVertex, out Edge<Vertex> edge))
                    list.Add(edge);
            }
            return list;
        }

        private static void RenderAlg_FormatVertex(object sender, QuikGraph.Graphviz.FormatVertexEventArgs<Vertex> args)
        {
            args.VertexFormat.Label = args.Vertex.ToString();

        }

        private static AdjacencyGraph<Vertex, Edge<Vertex>> ConstructGraph(MidValue[] values, GpReg[] dests, bool reverse, Dictionary<MidValue, GpReg> allocatedRegs)
        {
            AdjacencyGraph<Vertex, Edge<Vertex>> graph = new(false);
            for (int i = 0; i < Math.Min(values.Length, dests.Length); i++)
            {
                var value = values[i];
                var dest = dests[i];
                if (reverse)
                {
                    if (value.IsNull || dest == GpReg.RNull) continue;
                    else if (value.IsReg)
                    {
                        var actualDestReg = allocatedRegs[value];
                        // don't allow self-referencing edges
                        if (dest == actualDestReg) continue;
                        graph.AddVerticesAndEdge(new Edge<Vertex>(
                            new VertRegister(dest),
                            new VertRegister(actualDestReg)));
                    }
                    else throw new Exception();
                }
                else
                {
                    if (value.IsNull || dest == GpReg.RNull) continue;
                    else if (value.IsConstInt)
                    {
                        graph.AddVerticesAndEdge(new Edge<Vertex>(
                            new VertValue(value.GetIntValue()),
                            new VertRegister(dest)));
                    }
                    else if (value.IsConstBool)
                    {
                        graph.AddVerticesAndEdge(new Edge<Vertex>(
                            new VertValue(value.GetBoolValue() ? 1 : 0),
                            new VertRegister(dest)));
                    }
                    else if (value.IsReg)
                    {
                        var valueReg = allocatedRegs[value];
                        // don't allow self-referencing edges
                        if (dest == valueReg) continue;
                        graph.AddVerticesAndEdge(new Edge<Vertex>(
                            new VertRegister(valueReg),
                            new VertRegister(dest)));
                    }
                    else throw new Exception();
                }
            }
            return graph;
        }

        private abstract record Vertex;
        private record VertRegister(GpReg gpReg) : Vertex();
        private record VertValue(int iValue) : Vertex();

        internal abstract record MoveInstr;
        internal record MovIR(int value, GpReg dest) : MoveInstr();
        internal record MovRR(GpReg value, GpReg dest) : MoveInstr();
        internal record Swap(GpReg one, GpReg two) : MoveInstr();
    }
}
