using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace SuperLightGeneticAlgorithm
{
    public interface ISuperFitness
    {
        float Evaluate( SuperLightGA ga, float[] genome );
    }

    public class SuperLightGA
    {
        private static Random random = new Random( 0 );
        private float[] defaultChromosome = null;
        private float[][] bestGenomes = null;
        private float[] bestFitness = null;
        private float[][] populationGenomes = null;
        private float[] populationScores = null;
        private bool shouldSetInitialBest = true;

        public int Population = 50;
        public int SurvivalCount = 2; // Top 2 survive each generation
        public int ChromosomeCount = 1;
        public int GeneCount = 1;

        public float[] BestGenome
        {
            get
            {
                return bestGenomes[ 0 ];
            }
        }

        public float BestGenomeFitness
        {
            get
            {
                return bestFitness[ 0 ];
            }
        }

        public float[][] BestGenomes
        {
            get
            {
                return bestGenomes;
            }
        }

        public float[] BestGenomeFitnesses
        {
            get
            {
                return bestFitness;
            }
        }

        public SuperLightGA()
        {
        }

        #region Genome Methods

        public void GenerateDefaultGenome( float[] genome )
        {
            for( int i = 0; i < ChromosomeCount; i++ )
            {
                GenerateDefaultChromosome( genome, i );
            }
        }

        public void GenerateRandomGenome( float[] genome )
        {
            for( int i = 0; i < ChromosomeCount; i++ )
            {
                GenerateRandomChromosome( genome, i );
            }
        }

        public void PrintGenome( float[] genome )
        {
            for( int i = 0; i < ChromosomeCount; i++ )
            {
                Console.Error.Write( "(" );
                for( int j = 0; j < GeneCount; j++ )
                {
                    Console.Error.Write( "{0}", genome[ i * GeneCount + j ] );
                    if( j < GeneCount - 1 )
                    {
                        Console.Error.Write( " " );
                    }
                }
                Console.Error.Write( ")" );
            }
            Console.Error.WriteLine( "" );
        }

        #endregion

        #region Chromosome Methods

        public void SetDefaultChromosome( float[] genome )
        {
            defaultChromosome = (float[])genome.Clone();
        }

        public void GenerateRandomChromosome( float[] genome, int chromosome )
        {
            for( int i = 0; i < GeneCount; i++ )
            {
                genome[ chromosome * GeneCount + i ] = (float)random.NextDouble();
            }
        }

        public void GenerateDefaultChromosome( float[] genome, int chromosome )
        {
            if( defaultChromosome != null )
            {
                defaultChromosome.CopyTo( genome, chromosome * GeneCount );
            }
            else
            {
                GenerateRandomChromosome( genome, chromosome );
            }
        }

        public void MutateChromosome( float[] genome, int chromosome, float mutateBias = 1.0f )
        {
            for( int i = 0; i < GeneCount; i++ )
            {
                float r = (float)random.NextDouble();
                r = 1 - r * r;
                genome[ chromosome * GeneCount + i ] = (float)Math.Max( 0, Math.Min( genome[ chromosome * GeneCount + i ] - mutateBias * 0.5f + mutateBias * r, 1 ) );
            }
        }

        #endregion

        #region Gene Methods

        public float ReadGene( float[] genome, int chromosome, int gene )
        {
            return genome[ chromosome * GeneCount + gene ];
        }

        #endregion

        #region Genetic Algorithm Methods

        public void Initialize( int populationCount, int survivalCount, int numChromosomes, int numGenes )
        {
            Population = populationCount;
            SurvivalCount = survivalCount;
            ChromosomeCount = numChromosomes;
            GeneCount = numGenes;
            bestGenomes = new float[ SurvivalCount ][];
            populationGenomes = new float[ Population ][];
            for( int i = 0; i < SurvivalCount; i++ )
            {
                bestGenomes[ i ] = new float[ numChromosomes * numGenes ];
            }
            for( int i = 0; i < populationCount; i++ )
            {
                populationGenomes[ i ] = new float[ numChromosomes * numGenes ];
            }
            bestFitness = new float[ SurvivalCount ];
            populationScores = new float[ Population ];
        }

        public void ResetPopulation()
        {
            for( int i = 0; i < SurvivalCount; i++ )
            {
                for( int c = 0; c < ChromosomeCount; c++ )
                {
                    GenerateDefaultChromosome( bestGenomes[ i ], c );
                }
            }
        }

        public int Run( ISuperFitness evaluator, bool takeHighestEvaluation = true, int maxGenerations = 10000, int timeoutInMs = 100, bool shiftChromosome = true )
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            // Prepare for the generations
            if( shouldSetInitialBest )
            {
                float[] startGenome = new float[ ChromosomeCount * GeneCount ];
                GenerateDefaultGenome( startGenome );
                float score = evaluator.Evaluate( this, startGenome );
                for( int i = 0; i < SurvivalCount; i++ )
                {
                    Array.Copy( startGenome, 0, bestGenomes[ i ], 0, ChromosomeCount * GeneCount );
                    bestFitness[ i ] = score;
                }
                shouldSetInitialBest = false;
            }
            else if( shiftChromosome )
            {
                for( int i = 0; i < SurvivalCount; i++ )
                {
                    // Shift each chromosome forward to represent this next set
                    for( int c = 0; c < ChromosomeCount - 1; c++ )
                    {
                        for( int g = 0; g < GeneCount; g++ )
                        {
                            bestGenomes[ i ][ c * GeneCount + g ] = bestGenomes[ i ][ ( c + 1 ) * GeneCount + g ];
                        }
                    }
                    // Set the final turn as a starting chromosome
                    GenerateDefaultChromosome( bestGenomes[ i ], ChromosomeCount - 1 );
                    bestFitness[ i ] = evaluator.Evaluate( this, bestGenomes[ i ] );
                }
            }

            // Run Generations
            int generation = 0;
            bool shouldExit = false;
            for( generation = 0; maxGenerations < 0 || generation < maxGenerations; generation++ )
            {
                if( shouldExit )
                {
                    break;
                }

                // Generate & Score population
                // Add the best from the previous generation
                for( int p = 0; p < SurvivalCount; p++ )
                {
                    populationGenomes[ p ] = new float[ ChromosomeCount * GeneCount ];
                    Array.Copy( bestGenomes[ p ], 0, populationGenomes[ p ], 0, ChromosomeCount * GeneCount );
                    populationScores[ p ] = bestFitness[ p ];
                }
                // Set everything else too for when it times out
                for( int p = SurvivalCount; p < Population; p++ )
                {
                    int parent = random.Next( SurvivalCount );
                    populationGenomes[ p ] = new float[ ChromosomeCount * GeneCount ];
                    Array.Copy( bestGenomes[ parent ], 0, populationGenomes[ p ], 0, ChromosomeCount * GeneCount );
                    populationScores[ p ] = bestFitness[ 0 ];
                }
                float crossoverRate = Math.Min( 0.5f + (float)generation * 0.05f, 0.75f );
                float mutationRate = Math.Max( 0.5f - (float)generation * 0.02f, 0.01f );
                for( int p = SurvivalCount; p < Population; p++ )
                {
                    if( sw.ElapsedMilliseconds > timeoutInMs )
                    {
                        // Break out of loop due to timeout!
                        shouldExit = true;
                        break;
                    }
                    GenerateDefaultGenome( populationGenomes[ p ] );
                    for( int i = 0; i < ChromosomeCount; i++ )
                    {
                        // Crossover genes from one of the best parents
                        int parent = random.Next( SurvivalCount );
                        if( random.NextDouble() < crossoverRate )
                        {
                            for( int g = 0; g < GeneCount; g++ )
                            {
                                populationGenomes[ p ][ i * GeneCount + g ] = bestGenomes[ parent ][ i * GeneCount + g ];
                            }
                        }
                        // Mutate
                        else if( random.NextDouble() < mutationRate )
                        {
                            MutateChromosome( populationGenomes[ p ], i );
                        }
                    }
                    populationScores[ p ] = evaluator.Evaluate( this, populationGenomes[ p ] );
                }

                // Survive the top population
                Array.Sort( populationScores, populationGenomes );
                for( int p = 0; p < SurvivalCount; p++ ) // Add from previous generation/initial
                {
                    int srcIndex = takeHighestEvaluation ? Population - p - 1 : p;
                    Array.Copy( populationGenomes[ srcIndex ], 0, bestGenomes[ p ], 0, ChromosomeCount * GeneCount );
                    bestFitness[ p ] = populationScores[ srcIndex ];
                }
            }

            return generation;
        }

        #endregion
    }
}
