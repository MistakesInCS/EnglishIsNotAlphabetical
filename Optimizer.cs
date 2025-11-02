using System;
using System.Linq;

namespace Optimizer
{
    public class Optimizer
    {
        public Optimizer()
        {
            signMatrix = new double[matSize, matSize];
            permutationMatrix = new double[matSize, matSize];
            origPermMatrix = new double[matSize, matSize];
            for (int row = 0; row < matSize; row++)
            {
                permutationMatrix[row, row] = 1;
                for (int col = 0; col < matSize; col++)
                {
                    if (col < row)
                        signMatrix[row, col] = -1;
                    else if (col > row)
                        signMatrix[row, col] = 1;
                    else
                        signMatrix[row, col] = 0;
                }
            }
            return;
        }
        public int matSize = 26;
        public double[,] permutationMatrix, signMatrix, origPermMatrix;

        //https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle
        int[] randomPerm(int size)
        {
            Random r = new Random();
            int[] perm = new int[size];
            for (int i = 0; i < size; i++) { perm[i] = i; }
            while (size > 1)
            {
                size--;
                int j = r.Next(size + 1);
                (perm[j], perm[size]) = (perm[size], perm[j]);
            }

            return perm;
        }
        void initPermMatrix()
        {
            for (int row = 0; row < matSize; row++)
            {
                for (int col = 0; col < matSize; col++)
                {
                    permutationMatrix[row, col] = 0;
                }
            }
            int[] cols = randomPerm(matSize);
            for (int row = 0; row < matSize; row++)
            {
                permutationMatrix[row, cols[row]] = 1;
            }

        }
        public double[,] optimizeLoop(double[,] alphabetMatrix, double momentum = 0.3)
        {
            initPermMatrix();
            origPermMatrix = permutationMatrix;
            double maxIter = 20000;
            double[,] grad = new double[matSize, matSize];
            double[,] prev_vel = new double[matSize, matSize];
            double[,] new_vel = new double[matSize, matSize];

            for (double iteration = 0; iteration < maxIter; iteration++)
            {
                grad = lossGradientRealReal(alphabetMatrix, iteration, maxIter);
                new_vel = matAdd(MatMulConst(prev_vel, momentum), MatMulConst(grad, 1 - momentum));
                permutationMatrix = matAdd(permutationMatrix, new_vel);
            }

            return permutationMatrix;
        }

        public double[,] matAdd(double[,] a, double[,] b)
        {
            double[,] ret = new double[matSize, matSize];
            for (int row = 0; row < matSize; row++)
            {
                for (int col = 0; col < matSize; col++)
                {
                    ret[row, col] = a[row, col] + b[row, col];
                }
            }

            return ret;
        }

        double[,] lossGradientRealReal(double[,] alphabetMatrix, double iteration, double maxIter, double learningRate = .8)
        {
            double[,] grad = new double[matSize, matSize];

            // S^T P A + S P A^T
            double[,] scoreGrad = matAdd(matMul(matMul(transpose(signMatrix), permutationMatrix), alphabetMatrix),
                                          matMul(matMul(signMatrix, permutationMatrix), transpose(alphabetMatrix)));

            double[] rowSum = new double[matSize], colSum = new double[matSize];

            // Generate the row sums and col sums
            for (int row = 0; row < matSize; row++)
            {
                for (int col = 0; col < matSize; col++)
                {
                    rowSum[row] += permutationMatrix[row, col];
                    colSum[col] += permutationMatrix[row, col];
                }
            }

            double elemPen = 100;
            double iterRatio = iteration / maxIter;
            
            for (int row = 0; row < matSize; row++)
            {
                for (int col = 0; col < matSize; col++)
                {
                    grad[row, col] = (1 - iterRatio) * scoreGrad[row, col]
                    - elemPen * (Math.Pow(3, 1 + iterRatio * 10) * (fprime(permutationMatrix[row, col])
                                 + gprime(rowSum[row]) + gprime(colSum[col])));
                }
            }
            // Clip the gradient; the desired "distance" should be the current learning rate
            grad = MatMulConst(grad, 1 / Mat2Norm(ref grad) * learningRate * Math.Pow(.99, iteration));


            return grad;
        }

        double ScoreGradient(ref double[,] AP, ref double[,] PAt, int row, int col)
        {
            double grad = 0;
            for (int i = 0; i < matSize; i++)
            {
                grad += AP[col, i] * signMatrix[row, i];
                grad += PAt[i, row] * signMatrix[i, col];
            }
            return grad;
        }

        double Mat2Norm(ref double[,] mat)
        {
            double sum = 0;
            for (int row = 0; row < matSize; row++)
            {
                for (int col = 0; col < matSize; col++)
                {
                    sum += mat[row, col] * mat[row, col];
                }
            }
            return Math.Sqrt(sum);
        }

        double fprime(double x)
        {
            return x * (2 + x * (-6 + 4 * x)); // -(4x^3 - 6x^2 + 2x)
            //return 4 * x * x * x - 6 * x * x + 2 * x;
        }

        double gprime(double x)
        {
            return 2 * x - 2;
        }

        public double[,] GreedyValidPermutation(double[,] mat)
        {
            HashSet<int> rows = [];
            HashSet<int> cols = [];

            // Init hashsets
            for (int i = 0; i < matSize; i++)
            {
                rows.Add(i);
                cols.Add(i);
            }

            double[,] greedPerm = new double[matSize, matSize];

            // A simple but slow algorithm. Just find the max, set it to 1,
            // and then remove the row and col from the list of valid rows and cols.
            while (rows.Count != 0)
            {
                double max = mat[rows.ElementAt(0), cols.ElementAt(0)];
                (int, int) maxIndices = (rows.ElementAt(0), cols.ElementAt(0));
                foreach (int row in rows)
                {
                    foreach (int col in cols)
                    {
                        if (mat[row, col] > max)
                        {
                            max = mat[row, col];
                            maxIndices = (row, col);
                        }
                    }
                }
                rows.Remove(maxIndices.Item1);
                cols.Remove(maxIndices.Item2);
                greedPerm[maxIndices.Item1, maxIndices.Item2] = 1;
            }

            return greedPerm;
        }

        double elementPenalty(double[,] mat)
        {
            double sum = 0;
            foreach (double x in mat)
            {
                sum += -(x - 1) * x;
            }
            return sum;
        }

        double rowPenalty(double[,] mat)
        {
            double sum = 0;
            for (int i = 0; i < matSize; i++)
            {
                double rowSum = 0;
                for (int j = 0; j < matSize; j++)
                {
                    rowSum += mat[i, j];
                }
                sum += rowSum * (rowSum - 1) * (rowSum - 1);
            }
            return sum;
        }

        double colPenalty(double[,] mat)
        {
            return rowPenalty(transpose(mat));
        }

        double[,] transpose(double[,] mat)
        {
            double[,] transposeMat = new double[matSize, matSize];
            for (int row = 0; row < matSize; row++)
            {
                for (int col = 0; col < matSize; col++)
                {
                    transposeMat[col, row] = mat[row, col];
                }
            }
            return transposeMat;
        }

        public double[,] matMul(double[,] mat1, double[,] mat2)
        {
            double[,] result = new double[matSize, matSize];
            //Incrementing row
            for (int i = 0; i < matSize; i++)
            {
                //Incrementing col
                for (int j = 0; j < matSize; j++)
                {
                    //Actual sum
                    double sum = 0;
                    for (int k = 0; k < matSize; k++)
                    {
                        sum += mat1[i, k] * mat2[k, j];
                    }
                    result[i, j] = sum;
                }
            }
            return result;
        }
        public double frobenius(double[,] mat1, double[,] mat2)
        {
            double sum = 0;
            for (int row = 0; row < matSize; row++)
            {
                for (int col = 0; col < matSize; col++)
                    sum += mat1[row, col] * mat2[row, col];
            }
            return sum;
        }

        public double[,] MatMulConst(double[,] mat, double constant)
        {
            double[,] newMat = new double[matSize, matSize];
            for (int row = 0; row < matSize; row++)
            {
                for (int col = 0; col < matSize; col++)
                {
                    newMat[row, col] = mat[row, col] * constant;
                }
            }
            return newMat;
        }

        public double[,] RoundMatrix(double[,] mat, double cutoff)
        {
            double[,] newMat = new double[matSize, matSize];
            for (int row = 0; row < matSize; row++)
            {
                for (int col = 0; col < matSize; col++)
                {
                    if (mat[row, col] < cutoff)
                        newMat[row, col] = 0;
                    else
                        newMat[row, col] = 1;
                }
            }
            return newMat;
        }

        public double getScore(double[,] alphabetMatrix, double[,] perm)
        {
            return frobenius(matMul(matMul(perm, alphabetMatrix), transpose(perm)), signMatrix);
        }

        public bool IsPermutation(double[,] mat)
        {
            for (int i = 0; i < matSize; i++)
            {
                double rowSum = 0, colSum = 0;
                for (int j = 0; j < matSize; j++)
                {
                    rowSum += mat[i, j];
                    colSum += mat[j, i];
                }
                if (!((rowSum == 1) && (colSum == 1)))
                {
                    return false;
                }
            }
            return true;
        }

        public void DetailedComp(double[,] mat1, double[,] mat2)
        {
            for (int row = 0; row < matSize; row++)
            {
                for (int col = 0; col < matSize; col++)
                {
                    Console.WriteLine($"{mat1[row, col]} {mat2[row, col]}");
                }
            }
        }

        public string OutputOrder(double[,] perm, bool output = true)
        {
            string newOrder = "";
            for (int row = 0; row < matSize; row++)
            {
                for (int col = 0; col < matSize; col++)
                {
                    if (perm[row, col] == 1)
                    {
                        newOrder += (char)('a' + col);
                    }
                }
            }
            if(output)
                Console.WriteLine($"The new order is {newOrder}");
            return newOrder;
        }
    }
}