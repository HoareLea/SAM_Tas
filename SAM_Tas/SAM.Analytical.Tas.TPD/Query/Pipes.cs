using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    public static partial class Query
    {
        public static List<global::TPD.Pipe> Pipes(global::TPD.IPlantComponent plantComponent, Direction direction)
        {
            if(plantComponent == null)
            {
                return null;
            }

            int portCount = direction == Direction.Out ? (plantComponent as dynamic).GetOutputPortCount() : (plantComponent as dynamic).GetInputPortCount();
            if(portCount < 1)
            {
                return null;
            }

            List<global::TPD.Pipe> result = new List<global::TPD.Pipe>();
            for(int i = 1; i <= portCount; i ++)
            {
                int pipeCount = direction == Direction.Out ? (plantComponent as dynamic).GetOutputPipeCount(i) : (plantComponent as dynamic).GetInputPipeCount(i);
                if(pipeCount < 1)
                {
                    continue;
                }

                for (int j = 1; j <= pipeCount; j++)
                {
                    global::TPD.Pipe pipe = direction == Direction.Out ? (plantComponent as dynamic).GetOutputPipe(i, j) : (plantComponent as dynamic).GetInputPipe(i, j);

                    if(pipe == null)
                    {
                        continue;
                    }

                    result.Add(pipe);
                }

            }

            return result;
        }

        public static List<global::TPD.Pipe> Pipes(global::TPD.IPlantComponent plantComponent)
        {
            if(plantComponent == null)
            {
                return null;
            }

            List<global::TPD.Pipe> result = new List<global::TPD.Pipe>();

            List<global::TPD.Pipe> pipes = null;

            pipes = Pipes(plantComponent, Direction.In);
            if(pipes != null)
            {
                result.AddRange(pipes);
            }

            pipes = Pipes(plantComponent, Direction.Out);
            if (pipes != null)
            {
                result.AddRange(pipes);
            }

            return result;
        }
    }
}