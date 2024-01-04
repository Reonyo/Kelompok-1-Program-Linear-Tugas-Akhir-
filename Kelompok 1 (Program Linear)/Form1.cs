using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Kelompok_1__Program_Linear_
{
    public partial class form_main : Form
    {
        int jumlah_variabel, jumlah_constraint,kolom,baris,iterasi;
        string[] variabel_basis,variabel_nonbasis;
        double[,] A, B_invers,c_B,c,b,tabel_slack;
        private List<IterationData> iterationDataList;
        private const float AspectRatio = 16 / 9;

        public form_main()
        {
            InitializeComponent();
            iterasi = 0;
            iterationDataList = new List<IterationData>();
        }

        private void form_main_Resize(object sender, EventArgs e)
        {
            MaintainAspectRatio();
        }

        private void MaintainAspectRatio()
        {
            int targetWidth = this.ClientSize.Width;
            int targetHeight = (int)(targetWidth / AspectRatio);

            if (this.ClientSize.Height != targetHeight)
            {
                this.ClientSize = new System.Drawing.Size(targetWidth, targetHeight);
            }
        }

        private void btn_generatetable_Click(object sender, EventArgs e)
        {
            try
            {
                jumlah_variabel = int.Parse(txtbox_jumlahvariabel.Text);
                jumlah_constraint = int.Parse(txtbox_jumlahconstraint.Text);
                GenerateTable();
            }
            catch (FormatException)
            {
                MessageBox.Show("Input tidak valid. Silakan masukkan bilangan bulat yang valid.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        private void btn_calculate_Click(object sender, EventArgs e)
        {
            //Reset
            combobox_tabeliterasi.Items.Clear();
            iterationDataList.Clear();
            iterasi = 0;

            //Initialisasi Array
            c = new double[1,jumlah_variabel];
            A = new double[jumlah_constraint, jumlah_variabel];
            b = new double[jumlah_constraint,1];
            c_B = new double[1,jumlah_constraint];
            tabel_slack = new double[jumlah_constraint,jumlah_constraint];

            variabel_basis = new string[jumlah_constraint];
            variabel_nonbasis = new string[jumlah_variabel];
            B_invers = new double[jumlah_constraint,jumlah_constraint];

            //Assign Array
            try 
            {
                // Assign values for c array (Z row)
                for (int i = 1; i <= jumlah_variabel; i++)
                {
                    c[0,i - 1] = Convert.ToDouble(datagrid_userinput.Rows[0].Cells[i].Value);
                }

                // Assign values for A array (constraint rows)
                for (int i = 1; i <= jumlah_constraint; i++)
                {
                    for (int j = 1; j <= jumlah_variabel; j++)
                    {
                        A[i - 1, j - 1] = Convert.ToDouble(datagrid_userinput.Rows[i].Cells[j].Value);
                    }
                }

                // Assign values for b array (RHS)
                for (int i = 1; i <= jumlah_constraint; i++)
                {
                    b[i - 1,0] = Convert.ToDouble(datagrid_userinput.Rows[i].Cells[datagrid_userinput.ColumnCount - 1].Value);
                }

                // Assign B_invers sebagai matrix identitas
                for (int i = 0; i < jumlah_constraint; i++)
                {
                    for (int j = 0; j < jumlah_constraint; j++)
                    {
                        B_invers[i, j] = (i == j) ? 1.0 : 0.0;
                    }
                }
                // Assign tabel_slack sebagai matrix identitas
                for (int i = 0; i < jumlah_constraint; i++)
                {
                    for (int j = 0; j < jumlah_constraint; j++)
                    {
                        tabel_slack[i, j] = (i == j) ? 1.0 : 0.0;
                    }
                }

                // Assign values for variabel_basis
                variabel_basis = new string[jumlah_constraint];
                for (int i = 0; i < jumlah_constraint; i++)
                {
                    variabel_basis[i] = string.Concat("s",(i + 1).ToString());
                }

                // Assign values for variabel_nonbasis
                variabel_nonbasis = new string[jumlah_variabel];
                for (int i = 0; i < jumlah_variabel; i++)
                {
                    variabel_nonbasis[i] = string.Concat("x",(i + 1).ToString());
                }

                // Langkah Awal selesai..
                // Lanjut Uji Optimalitas pertama
                double[] minus_c;
                int minNegativeindex=-1;
                double minNegativeValue = double.MaxValue;
                minus_c = new double[jumlah_variabel];
                for (int i = 0; i < jumlah_variabel; i++)
                {
                    minus_c[i] = -c[0,i];
                    if (minus_c[i] < 0 && minus_c[i]<minNegativeValue)
                    {
                        minNegativeValue = minus_c[i];
                        minNegativeindex = i;
                    }
                }
                if(minNegativeindex==-1)
                {
                    // Sudah Optimal
                    return;
                }
                // Kolom dengan index minNegativeIndex menjadi pivot
                kolom = minNegativeindex+1;

            }
            catch (FormatException)
            {
                MessageBox.Show("Input tidak valid. Silakan masukkan bilangan bulat yang valid.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
            Iteration();
            iterasi += 1;

            combobox_tabeliterasi.Items.Add($"Iterasi ke-{iterasi}");
            IterationData iterationData = new IterationData
            {
                Basis = variabel_basis,
                A = CopyMatrix(A),
                TabelSlack = CopyMatrix(tabel_slack),
                B = CopyMatrix(b),
            };
            iterationDataList.Add(iterationData);
        }
        private void Iteration()
        {
            string var_masuk, var_keluar;
            double[] a,rasio,miu;
            double[,] E;
            E = new double[jumlah_constraint, jumlah_constraint];
            a = new double[jumlah_constraint];
            rasio = new double[jumlah_constraint];
            miu = new double[jumlah_constraint];

            // Assign B_invers sebagai matrix identitas
            for (int i = 0; i < jumlah_constraint; i++)
            {
                for (int j = 0; j < jumlah_constraint; j++)
                {
                    B_invers[i, j] = (i == j) ? 1.0 : 0.0;
                }
            }
            //Save tabel
            IterationData iterationData = new IterationData
            {
                Basis = CopyArray(variabel_basis),
                A = CopyMatrix(A),
                TabelSlack = CopyMatrix(tabel_slack),
                B = CopyMatrix(b),
            };
            iterationDataList.Add(iterationData);

            //Variabel Masuk
            for (int i = 0;i<jumlah_constraint;i++)
            {
                a[i] = A[i, kolom-1];
                //MessageBox.Show($"nilai a ke-{i}= {a[i]}");
            }

            if(kolom==-1)
            {
                MessageBox.Show("The problem is unbounded");
                return;
            }
            var_masuk = variabel_nonbasis[kolom-1];
           
            for(int i = 0;i<jumlah_constraint;i++)
            {
                if (a[i]==0)
                {
                    rasio[i] = 0;
                }
                else
                {
                    rasio[i] = (b[i,0] / a[i]);
                }
                //MessageBox.Show($"nilai rasio ke-{i}= {rasio[i]}");
            }
            //Sekarang pilih nilai terkecil tak negatif/tak nol
            baris = -1;
            double minNegativeValue = double.MaxValue;
            for(int i = 0;i<jumlah_constraint;i++)
            {
                if (rasio[i] > 0 && rasio[i] < minNegativeValue)
                {
                    minNegativeValue = rasio[i];
                    baris = i+1;
                }
            }
            if(baris==-1)
            {
                MessageBox.Show("The problem is infeasible");
                return;
            }
            //Variabel Keluar (r = baris)
            var_keluar = variabel_basis[baris-1];

            //Edit Nilai variabel_basis dan variabel_nonbasis
            variabel_basis[baris-1] = var_masuk;
            variabel_nonbasis[kolom-1] = var_keluar;
            c_B[0,baris-1] = c[0,kolom-1];

            //Menentukan B_invers baru
            for(int i =1;i<=jumlah_constraint;i++)
            {
                if(i==baris)
                {
                    miu[i-1] = 1 / (A[baris-1,kolom-1]);
                }
                else
                {
                    miu[i-1] = -A[i-1, kolom-1] / A[baris-1, kolom-1];
                }
            }
            for (int i = 0; i < jumlah_constraint; i++)
            {
                for (int j = 0; j < jumlah_constraint; j++)
                {
                    if(j==baris-1)
                    {
                        E[i, j] = miu[i];
                    }
                    else if(i==j)
                    {
                        E[i, j] = 1;
                    }
                    else
                    {
                        E[i, j] = 0;
                    }
                }
            }
            //B_invers baru
            B_invers = MultiplyMatrices(E, B_invers);

            //Uji optimalitas
            (bool optimal,int index) = UjiOptimal();
            kolom = index+1;

            A = MultiplyMatrices(E, A);
            b = MultiplyMatrices(E, b);
            tabel_slack = MultiplyMatrices(E, tabel_slack);

            //Rekursif
            iterasi += 1;
            combobox_tabeliterasi.Items.Add($"Iterasi ke-{iterasi}");
            if (optimal == false)
            {
                Iteration();
            }
            else
            {
                double[,] Z_maks;
                Z_maks = MultiplyMatrices(c_B, b);

                // Calculate solution variables based on the last iteration's basis
                double[] solutionVariables = new double[jumlah_variabel];
                for (int i = 0; i < solutionVariables.Length; i++)
                {
                    int basisIndex = Array.IndexOf(variabel_basis, $"x{i + 1}");
                    if (basisIndex != -1)
                    {
                        solutionVariables[i] = b[basisIndex, 0];
                    }
                    else
                    {
                        solutionVariables[i] = 0; //non basis dianggap nol
                    }
                }

                // Display Z max and solution variables
                StringBuilder resultMessage = new StringBuilder();
                resultMessage.AppendLine($"Z maks adalah {Z_maks[0, 0]}");

                for (int i = 0; i < solutionVariables.Length; i++)
                {
                    resultMessage.AppendLine($"x{i + 1} = {solutionVariables[i]}");
                }

                MessageBox.Show(resultMessage.ToString());
            }
        }
        private double[,] CopyMatrix(double[,] matrix)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            double[,] copy = new double[rows, cols];
            Array.Copy(matrix, copy, matrix.Length);
            return copy;
        }
        private string[] CopyArray(string[] source)
        {
            return source.ToArray();
        }
        private void btn_tampilkantabel_Click(object sender, EventArgs e)
        {
            if (combobox_tabeliterasi.SelectedIndex >= 0 && combobox_tabeliterasi.SelectedIndex < iterationDataList.Count)
            {
                IterationData selectedIteration = iterationDataList[combobox_tabeliterasi.SelectedIndex];

                // Clear existing columns and rows
                datagrid_useroutput.Columns.Clear();
                datagrid_useroutput.Rows.Clear();

                // Create the column headers
                List<string> columnHeaders = new List<string> { "Basis" };
                for (int i = 1; i <= selectedIteration.A.GetLength(1); i++)
                {
                    columnHeaders.Add("x" + i);
                }
                // Add slack variable columns
                for (int i = 1; i <= selectedIteration.TabelSlack.GetLength(1); i++)
                {
                    columnHeaders.Add("s" + i);
                }
                columnHeaders.Add("RHS");

                // Manually add columns to the DataGridView
                foreach (string header in columnHeaders)
                {
                    DataGridViewColumn column = new DataGridViewTextBoxColumn();
                    column.HeaderText = header;
                    column.ReadOnly = true; // Make all columns read-only
                    column.Width = 40;
                    datagrid_useroutput.Columns.Add(column);
                }

                // Add rows to the DataGridView
                for (int i = 0; i < selectedIteration.A.GetLength(0); i++)
                {
                    DataGridViewRow row = new DataGridViewRow();
                    row.CreateCells(datagrid_useroutput);

                    row.Cells[0].Value = selectedIteration.Basis[i];
                    for (int j = 1; j <= selectedIteration.A.GetLength(1); j++)
                    {
                        row.Cells[j].Value = selectedIteration.A[i, j - 1];
                    }

                    // Add slack variable values
                    for (int j = 1; j <= selectedIteration.TabelSlack.GetLength(1); j++)
                    {
                        row.Cells[selectedIteration.A.GetLength(1) + j].Value = selectedIteration.TabelSlack[i, j - 1];
                    }
                    row.Cells[row.Cells.Count - 1].Value = selectedIteration.B[i, 0];
                    datagrid_useroutput.Rows.Add(row);
                }
            }
            else
            {
                MessageBox.Show("Please select a valid iteration.", "Invalid Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        static double[,] SubtractMatrices(double[,] matrixA, double[,] matrixB)
        {
            int rowsA = matrixA.GetLength(0);
            int colsA = matrixA.GetLength(1);
            int rowsB = matrixB.GetLength(0);
            int colsB = matrixB.GetLength(1);

            // Check if matrices have the same dimensions
            if (rowsA != rowsB || colsA != colsB)
            {
                Console.WriteLine("Gagal kurang");
                throw new ArgumentException("Matrices must have the same dimensions for subtraction.");
            }

            // Initialize the result matrix
            double[,] resultMatrix = new double[rowsA, colsA];

            // Perform matrix subtraction
            for (int i = 0; i < rowsA; i++)
            {
                for (int j = 0; j < colsA; j++)
                {
                    resultMatrix[i, j] = matrixA[i, j] - matrixB[i, j];
                }
            }

            return resultMatrix;
        }
        private void TampilkanMatrix(double[,] matrix,string judul)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                   MessageBox.Show($"{judul} baris ke-{i} Kolom ke-{j} = {matrix[i,j]}");
                }
            }
        }
        private (bool optimal, int index) UjiOptimal()
        {
            int index;
            bool ada_slack,ada_variabel,optimal;
            double minNegativeValue = double.MaxValue;
            ada_slack = false;
            ada_variabel = false;
            optimal = true;
            index = -1;
            foreach (var variable in variabel_nonbasis)
            {
                if (variable.StartsWith("s"))
                {
                    ada_slack = true;
                }
                if (variable.StartsWith("x"))
                {
                    ada_variabel = true;
                }
            }
            if(ada_variabel==true)
            {
                double[,] temporary;
                temporary = MultiplyMatrices(c_B, B_invers);
                temporary = MultiplyMatrices(temporary, A);
                temporary = SubtractMatrices(temporary, c);
                for (int i = 0; i < jumlah_variabel; i++)
                {
                    if (temporary[0, i] < 0 && temporary[0,i]<=minNegativeValue)
                    {
                        minNegativeValue = temporary[0,i];
                        index = i;
                        optimal = false;
                    }
                }
            }
            if(ada_slack==true)
            {
                double[,] temporary;
                temporary = MultiplyMatrices(c_B, B_invers);
                for (int i = 0; i < jumlah_variabel; i++)
                {
                    if (temporary[0, i] < 0)
                    {
                        optimal = false;
                    }
                }
            }
            return (optimal,index);
        }
        private double[,] MultiplyMatrices(double[,] matrixA, double[,] matrixB)
        {
            int rowsA = matrixA.GetLength(0);
            int colsA = matrixA.GetLength(1);
            int rowsB = matrixB.GetLength(0);
            int colsB = matrixB.GetLength(1);

            // Check if matrices can be multiplied
            if (colsA != rowsB)
            {
                Console.WriteLine("Gagal kali");
                throw new ArgumentException("Invalid matrix dimensions for multiplication.");
            }

            // Initialize the result matrix
            double[,] resultMatrix = new double[rowsA, colsB];

            // Perform matrix multiplication
            for (int i = 0; i < rowsA; i++)
            {
                for (int j = 0; j < colsB; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < colsA; k++)
                    {
                        sum += matrixA[i, k] * matrixB[k, j];
                    }
                    resultMatrix[i, j] = sum;
                }
            }

            return resultMatrix;
        }
        private void GenerateTable()
        {
            // Clear existing columns and rows
            datagrid_userinput.Columns.Clear();
            datagrid_userinput.Rows.Clear();

            // Create the column headers
            List<string> columnHeaders = new List<string> { "Basis" };
            for (int i = 1; i <= jumlah_variabel; i++)
            {
                columnHeaders.Add("x" + i);
            }
            columnHeaders.Add("sign");
            columnHeaders.Add("RHS");

            // Manually add columns to the DataGridView
            foreach (string header in columnHeaders)
            {
                DataGridViewColumn column = new DataGridViewTextBoxColumn();
                column.HeaderText = header;

                // Set specific columns as read-only
                if (header == "Basis" || header == "sign")
                {
                    column.ReadOnly = true;
                }
                column.Width = 40;
                datagrid_userinput.Columns.Add(column);
            }

            // Create Z row
            DataGridViewRow zRow = new DataGridViewRow();
            zRow.CreateCells(datagrid_userinput);
            zRow.Cells[0].Value = "Z";
            zRow.Cells[zRow.Cells.Count - 2].ReadOnly=true;
            zRow.Cells[zRow.Cells.Count - 1].ReadOnly=true;

            datagrid_userinput.Rows.Add(zRow);

            // Create constraint rows
            for (int i = 1; i <= jumlah_constraint; i++)
            {
                DataGridViewRow constraintRow = new DataGridViewRow();
                constraintRow.CreateCells(datagrid_userinput);
                constraintRow.Cells[0].Value = "c" + i;
                constraintRow.Cells[constraintRow.Cells.Count - 2].Value = "<=";
                constraintRow.Cells[constraintRow.Cells.Count - 1].Value = 0;

                datagrid_userinput.Rows.Add(constraintRow);
            }
        }
    }
    public class IterationData
    {
        public string[] Basis { get; set; }
        public double[,] A { get; set; }
        public double[,] TabelSlack { get; set; }
        public double[,] B { get; set; }
    }
}
