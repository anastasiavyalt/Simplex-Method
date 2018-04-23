﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace SimplexMethod
{
    class Simplex
    {
        public LinearProgrammingProblem Prob { get; set; }
        public double[,] SimplexTable { get; set; }
        // Симплексная таблица:
        // 0 строка - коэффициенты при переменных в целевой функции
        // 1-n строки 1-m столбцы - коэффициенты при переменных в соответсвующих ограничениях
        // n+1 строка - оценки
        // n+2 строка - оценки: коэффициент при M
        // 0 столбец - свободные члены
        // Базис хранится отдельно
        public int[] Basis { get; set; }
        public List<int> SlackVarInd { get; set; }
        public List<int> SurplusVarInd { get; set; }
        public List<int> ArtificialVarInd { get; set; }
        private int varInd;
        private int costMRow;
        private int costRow;
        private const double M = double.PositiveInfinity;

        public Simplex(LinearProgrammingProblem prob)
        {
            Prob = prob;
            SimplexTable = new double[Prob.CountConstraint + 3,
                Prob.CountVariables + CountTotalNewVars() + 1];
            varInd = Prob.CountVariables + 1;
            costMRow = SimplexTable.GetLength(0) - 1;
            costRow = costMRow - 1;
            Basis = new int[Prob.CountConstraint];
            for (int i = 0; i < Prob.CountConstraint + 1; i++)
            {
                for (int j = 0; j < Prob.CountVariables + 1; j++)
                {
                    if (i == 0 && j != 0)
                    {
                        SimplexTable[0, j] = prob.CriteriaCoefficients[j - 1];
                    }
                    else if(i != 0 && j == 0)
                    {
                        SimplexTable[i, 0] = prob.Constants[i - 1];
                    }
                    else if (i == 0 && j == 0)
                    {
                        continue;
                    }
                    else {
                        SimplexTable[i, j] = prob.ConstraintCoefficients[i - 1, j - 1];
                    }
                }
            }

        }
        
        public int CountTotalNewVars()
        {
            int count = 0;
            for(int i = 0; i < Prob.CountConstraint; i++)
            {
                if((Prob.ConstraintSigns[i] == MathSign.LessThan && Prob.Constants[i] >= 0) || 
                    (Prob.ConstraintSigns[i] == MathSign.GreaterThan && Prob.Constants[i] < 0))
                {
                    count++;
                }
                else if((Prob.ConstraintSigns[i] == MathSign.GreaterThan && Prob.Constants[i] >= 0) ||
                    (Prob.ConstraintSigns[i] == MathSign.LessThan && Prob.Constants[i] < 0))
                {
                    count += 2;
                }
                else if(Prob.ConstraintSigns[i] == MathSign.Equal)
                {
                    count++;
                }
            }
            if(Prob.NotNonNegativeVarInd != null)
            {
                count++;
            }
            return count;
        }

        public void ConvertToStandardForm()
        {
            ToMax();
            MakeConstantsPositive();
            MakeAllVarNonNegative();
            AddSlackAndSurplusVariables();
        }
        
        public void ToMax()
        {
            if (Prob.Maximize == false)
            {
                for (int i = 1; i < Prob.CountVariables + 1; i++)
                {
                    SimplexTable[0,i] *= -1;
                }
            }
        }
        
        public void MakeConstantsPositive()
        {
            for (int i = 0; i < Prob.CountConstraint; i++)
            {
                if (Prob.Constants[i] <= 0)
                {
                    for(int j = 0; j < SimplexTable.GetLength(1); j++)
                    {
                        SimplexTable[i + 1, j] *= -1;
                    }
                }
            }
        }
        
        public void MakeAllVarNonNegative()
        {
            if (Prob.NotNonNegativeVarInd != null)
            {
                double coeffCrit = 0;
                double coeffConstr = 0;
                for (int i = 0; i < Prob.CountConstraint + 1; i++)
                {
                    foreach (int ind in Prob.NotNonNegativeVarInd)
                    {
                        if (i == 0)
                        {
                            coeffCrit -= SimplexTable[0, ind + 1];
                        }
                        coeffConstr -= SimplexTable[i, ind + 1];
                    }
                    SimplexTable[i, varInd] = coeffConstr;
                    coeffConstr = 0;
                }
                SimplexTable[0, varInd] = coeffCrit;
                varInd++;
            }
        }
                
        public void AddSlackAndSurplusVariables()
        {
            SlackVarInd = new List<int>();
            SurplusVarInd = new List<int>();
            for (int i = 0; i < Prob.CountConstraint; i++)
            {
                if ((Prob.ConstraintSigns[i] == MathSign.LessThan && Prob.Constants[i] >= 0) ||
                    (Prob.ConstraintSigns[i] == MathSign.GreaterThan && Prob.Constants[i] < 0))
                {
                    SlackVarInd.Add(varInd);
                    SimplexTable[i + 1, varInd] = 1;
                    Basis[i] = varInd;
                    varInd++;
                }
                else if ((Prob.ConstraintSigns[i] == MathSign.GreaterThan && Prob.Constants[i] >= 0) ||
                    (Prob.ConstraintSigns[i] == MathSign.LessThan && Prob.Constants[i] < 0))
                {
                    SurplusVarInd.Add(varInd);
                    SimplexTable[i + 1, varInd] = -1;
                    varInd++;
                }
            }
        }

        public void AddArtificialVariables()
        {
            ArtificialVarInd = new List<int>();
            for (int i = 0; i < Prob.CountConstraint; i++)
            {
                if ((Prob.ConstraintSigns[i] == MathSign.GreaterThan && Prob.Constants[i] >= 0) ||
                    (Prob.ConstraintSigns[i] == MathSign.LessThan && Prob.Constants[i] < 0) ||
                    Prob.ConstraintSigns[i] == MathSign.Equal)
                {
                    ArtificialVarInd.Add(varInd);
                    SimplexTable[i + 1, varInd] = 1;
                    SimplexTable[0, varInd] = -M;
                    Basis[i] = varInd;
                    varInd++;
                }
            }
        }

        private void FindCosts()
        {
            int costMRow = SimplexTable.GetLength(0) - 1;
            int costRow = costMRow - 1;
            for (int j = 0; j < SimplexTable.GetLength(1); j++)
            {
                SimplexTable[costRow, j] = 0;
                SimplexTable[costMRow, j] = 0;
                for (int i = 1; i < SimplexTable.GetLength(0) - 2; i++)
                {
                    if (SimplexTable[0, Basis[i - 1]] == -M)
                    {
                        SimplexTable[costMRow, j] += (SimplexTable[i, j] * (-1));
                    }
                    else
                    {
                        SimplexTable[costRow, j] += (SimplexTable[i, j] * SimplexTable[0, Basis[i - 1]]);
                    }
                }
                if (SimplexTable[0, j] == -M)
                {
                    SimplexTable[costMRow, j] += 1;
                }
                else
                {
                    SimplexTable[costRow, j] -= SimplexTable[0, j];
                }
            }
        }

        public Tuple<double, double[]> Calculate()
        {
            ConvertToStandardForm();
            AddArtificialVariables();
            Console.WriteLine();
            Console.WriteLine("INIT BASIS");
            foreach (int i in Basis)
            {
                Console.Write(i + " ");
            }
            FindCosts();
            Console.WriteLine();
            Console.WriteLine("INIT TABLE");
            for (int i = 0; i < SimplexTable.GetLength(0); i++)
            {
                for (int j = 0; j < SimplexTable.GetLength(1); j++)
                {
                    Console.Write(SimplexTable[i, j] + "   ");
                }
                Console.WriteLine();
            }
            while (true)
            {
                int pivotColumn = FindPivotColumn();
                Console.WriteLine("PIV COL: " + pivotColumn);
                if (pivotColumn == -1)
                {
                    // Проверка на равентсво нулю исскуственной переменной: если нет, то нет оптимального решения
                    if (SimplexTable[SimplexTable.GetLength(0) - 1, 0] != 0)
                    {
                        return null;
                    }
                    else
                    {
                        double[] optSolution = new double[SimplexTable.GetLength(1)-1];
                        foreach(int i in Basis)
                        {
                            optSolution[i - 1] = SimplexTable[i, 0];
                        }
                        double optObjectiveValue = SimplexTable[SimplexTable.GetLength(0) - 2, 0];
                        Console.WriteLine("OPT SOL:");
                        foreach(double v in optSolution)
                        {
                            Console.Write(v + " ");
                        }
                        Console.WriteLine();
                        Console.WriteLine("OPT VAL:");
                        Console.WriteLine(optObjectiveValue);
                        return new Tuple<double, double[]>(optObjectiveValue, optSolution);
                    }
                }
                else
                {
                    int pivotRow = FindPivotRow(pivotColumn);
                    Console.WriteLine("PIV ROW: " + pivotRow);
                    if (pivotRow == -1)
                    {
                        return null;
                    }
                    Basis[pivotRow - 1] = pivotColumn;
                    Console.WriteLine("NEW BASE: ");
                    foreach (int i in Basis)
                    {
                        Console.Write(i + " ");
                    }
                    Console.WriteLine();
                    double[,] oldTable = new double[SimplexTable.GetLength(0), SimplexTable.GetLength(1)]; 
                    Array.Copy(SimplexTable, oldTable, SimplexTable.Length);
                    double pivotElem = SimplexTable[pivotRow, pivotColumn];
                    for (int j = 0; j < SimplexTable.GetLength(1); j++)
                    {
                        SimplexTable[pivotRow, j] = oldTable[pivotRow, j] / pivotElem;
                    }
                    for (int i = 1; i < SimplexTable.GetLength(0); i++)
                    {
                        if (i == pivotRow || oldTable[i, pivotColumn] == 0)
                            continue;
                        for (int j = 0; j < SimplexTable.GetLength(1)-2; j++)
                        {
                            SimplexTable[i, j] = oldTable[i, j] - (oldTable[i, pivotColumn] * SimplexTable[pivotRow,j]);
                        }
                    }
                    FindCosts();
                    Console.WriteLine();
                    Console.WriteLine("NEW TABLE");
                    for (int i = 0; i < SimplexTable.GetLength(0); i++)
                    {
                        for (int j = 0; j < SimplexTable.GetLength(1); j++)
                        {
                            Console.Write(String.Format("{0:0.##}",SimplexTable[i, j]) + "        ");
                        }
                        Console.WriteLine();
                    }
                    
                }
            }
        }

        // rewrite later
        private int FindPivotColumn()
        {
            List<int> minInd = new List<int>();
            double minM = SimplexTable[costMRow, 1];
            for (int j = 2; j < SimplexTable.GetLength(1); j++)
            {
                if(SimplexTable[costMRow, j] < minM)
                {
                    minM = SimplexTable[costMRow, j];
                }
            }
            for (int j = 1; j < SimplexTable.GetLength(1); j++)
            {
                if (SimplexTable[costMRow, j] == minM)
                {
                    minInd.Add(j);
                }
            }
            if(minInd.Count == 1)
            {
                return minInd[0];
            }
            else
            {
                double min = SimplexTable[costRow, 1];
                int pivotCol = minInd[0];
                foreach(int j in minInd)
                {
                    if (SimplexTable[costRow, j] < min)
                    {
                        min = SimplexTable[costRow, j];
                        pivotCol = j;
                    }
                }
                Console.WriteLine("pivcol " + pivotCol+" " + SimplexTable[costRow, pivotCol]);
                Console.WriteLine();
                Console.WriteLine("min " + min + " minM " + minM);
                if(min >= 0 && minM >=0)
                {
                    return -1;
                }
                else 
                    return pivotCol;
            }
        }

        private int FindPivotRow(int pivotCol)
        {
            int pivotRow = -1;
            for (int i = 1; i < SimplexTable.GetLength(0) - 2; i++)
            {
                if(SimplexTable[i, pivotCol] > 0)
                {
                    pivotRow = i;
                    break;
                }
            }
            if (pivotRow == -1)
                return pivotRow;
            for(int i = pivotRow + 1; i < SimplexTable.GetLength(0) - 2; i++)
            {
                if((SimplexTable[i, pivotCol] > 0) && ((SimplexTable[i, 0] / SimplexTable[i, pivotCol]) < 
                    (SimplexTable[pivotRow, 0] / SimplexTable[pivotRow, pivotCol])))
                {
                    pivotRow = i;
                }
            }
            return pivotRow;
        }
    }
}
