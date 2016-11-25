using System;
using SuperLightGeneticAlgorithm;

namespace SimpleExample
{
    // This finds the quickest "jumps" to get from a random spot to the target
    public class JumpFinder : ISuperFitness
    {
        public const int MaxSteps = 5;
        public const int MaxJumpDistance = 1000;
        public int Start { get; set; }
        public int Target { get; set; }

        public JumpFinder( int target )
        {
            this.Target = target;
            this.Start = target - MaxJumpDistance * MaxSteps + new Random().Next( 2 * MaxJumpDistance * MaxSteps );
        }

        public int Perform( int number, float g1, float g2 )
        {
            int factor = (int)Math.Round( g1 * MaxJumpDistance );
            if( g2 < 0.5f ) // Subtract
            {
                return number - factor;
            }
            else // Add
            {
                return number + factor;
            }
        }

        public string Debug( SuperLightGA ga, float[] genome )
        {
            int start = Start;
            string output = "";
            for( int i = 0; i < MainClass.NumberOfSteps; i++ )
            {
                float g1 = ga.ReadGene( genome, i, 0 );
                float g2 = ga.ReadGene( genome, i, 1 );
                int factor = (int)Math.Round( g1 * MaxJumpDistance );
                if( g2 < 0.5f ) // Subtract
                {
                    output += string.Format( "- {0} ", factor );
                }
                else // Add
                {
                    output += string.Format( "+ {0} ", factor );
                }
                start = Perform(
                    start,
                    ga.ReadGene( genome, i, 0 ),
                    ga.ReadGene( genome, i, 1 )
                );
                if( start == Target )
                {
                    break;
                }
            }
            return output + string.Format( "= {0} ", start );
        }

        public float Evaluate( SuperLightGA ga, float[] genome )
        {
            int start = Start;
            int numberOfSteps;
            int score = 0;
            for( numberOfSteps = 0; numberOfSteps < MainClass.NumberOfSteps; numberOfSteps++ )
            {
                start = Perform(
                    start,
                    ga.ReadGene( genome, numberOfSteps, 0 ),
                    ga.ReadGene( genome, numberOfSteps, 1 )
                );
                if( start == Target )
                {
                    break;
                }
                // Evaluate based on # of jumps and distance from goal
                score += ( numberOfSteps + 1 ) * Math.Max( 0, Math.Abs( Target - start ) );
            }
            return score;
        }
    }

    public class MainClass
    {
        public const int NumberOfSteps = 5;

        public static void Main( string[] args )
        {
            var jumpyFrog = new JumpFinder( 1234 );
            Console.WriteLine( "Target: {0}", jumpyFrog.Target );
            Console.WriteLine( "Start: {0}", jumpyFrog.Start );

            var ga = new SuperLightGA();
            // Population of 50. Keep Top-2 from each generation
            ga.Initialize( 50, 2, JumpFinder.MaxSteps, 2 );
            int generations = ga.Run( jumpyFrog, false );
            Console.WriteLine( "Generations: {0}", generations );
            for( int i = 0; i < ga.SurvivalCount; i++ )
            {
                Console.WriteLine( "  {0}", jumpyFrog.Debug( ga, ga.BestGenomes[ i ] ) );
                Console.WriteLine( " Score: {0}", ga.BestGenomeFitnesses[ i ] );
            }
            Console.ReadKey();
        }
    }
}
