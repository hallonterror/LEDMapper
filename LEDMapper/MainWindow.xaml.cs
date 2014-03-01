using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LEDMapper
{
    public class GridButton : Button
    {
        public GridButton()
        {
            SetResourceReference(Control.StyleProperty, "LEDButton");
            BackgroundChoices = new SolidColorBrush[2] { new SolidColorBrush(Colors.WhiteSmoke), (SolidColorBrush)(new BrushConverter().ConvertFrom("#CCE51400")) };
            isActive = false;
        }

        public void SetCoordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X
        {
            get { return (int)GetValue(Grid.ColumnProperty); }
            set { SetValue(Grid.ColumnProperty, value); }
        }

        public int Y
        {
            get { return (int)GetValue(Grid.RowProperty); }
            set { SetValue(Grid.RowProperty, value); }
        }

        public Boolean isActive
        {
            get { return _isActive; }
            set
            {
                _isActive = value;
                Background = BackgroundChoices[Convert.ToInt16(_isActive)];
            }
        }

        SolidColorBrush[] BackgroundChoices;
        bool _isActive;
    }

    public partial class MainWindow : MahApps.Metro.Controls.MetroWindow
    {
        public enum ERROR_CODE { ALL_OK = 0, LOAD_WARNING_FILE_NOT_FOUND = 100, LOAD_ERROR_NOT_ENOUGH_LINES = 1000, LOAD_ERROR_LINES_UNALIGNED };

        public GridButton[,] buttons;
        int maps = 10;
        int CurrentMapIndex = -1;

        public MainWindow()
        {
            InitializeComponent();

            buttons = new GridButton[8, 8];
            PopulateGrid(buttons);
            AddMapSelection(maps);

            LogMessage("Welcome to the LED light editor.");
        }

        public void AddMapSelection(int num)
        {
            for (int i = 1; i <= num; i++)
                MatrixSelection.Items.Add(i);
        }

        public void PopulateGrid(GridButton[,] buttons)
        {
            int rows = buttons.GetLength(0);
            int cols = buttons.GetLength(1);

            // Add rows for buttons
            for (int y = 0; y < rows; y++)
            {
                RowDefinition newRow = new RowDefinition();
                ButtonGrid.RowDefinitions.Add(newRow);
            }

            // Add columns for buttons
            for (int x = 0; x < cols; x++)
            {
                ColumnDefinition newCol = new ColumnDefinition();
                ButtonGrid.ColumnDefinitions.Add(newCol);
            }

            // Add buttons
            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    buttons[x, y] = new GridButton();
                    buttons[x, y].SetCoordinate(x, y);
                    buttons[x, y].Click += new RoutedEventHandler(GridClick);
                    ButtonGrid.Children.Add(buttons[x, y]);
                }
            }
        }

        public String CreateString(GridButton[,] buttons, String mapname)
        {
            String output = "bool " + mapname + " [" + buttons.Length.ToString() + "] = {";
            for (int y = 0; y < buttons.GetLength(0); y++)
            {
                output += "\n\t";
                for (int x = 0; x < buttons.GetLength(1); x++)
                {
                    output += Convert.ToInt16(buttons[x, y].isActive).ToString() + ", ";
                }
            }
            output = output.Remove(output.Length - 2);
            output += "\n};\n";
            return output;
        }

        public void SaveMap(GridButton[,] buttons, int mapSelection)
        {
            System.IO.Directory.CreateDirectory(@"Maps/");
            System.IO.File.WriteAllText(@"Maps/Map - " + mapSelection.ToString() + ".h",
                                        CreateString(buttons, "map" + mapSelection.ToString()));

        }
        public ERROR_CODE LoadMap(GridButton[,] buttons, int mapSelection)
        {
            try
            {
                using (System.IO.StreamReader file = new System.IO.StreamReader(@"Maps/Map - " + mapSelection.ToString() + ".h"))
                {
                    String line;
                    Char[] delim = { ',' };
                    Char[] remove = { ' ', '\t', '\n' };

                    // Ignore the first line
                    line = file.ReadLine();

                    // Read rows of the file
                    for (int r = 0; r < buttons.GetLength(0); r++)
                    {
                        // If line fails to be read
                        if ((line = file.ReadLine()) == null)
                            return ERROR_CODE.LOAD_ERROR_NOT_ENOUGH_LINES;

                        // Clean up data
                        line = line.Trim(remove);
                        String[] res = line.Split(delim);

                        // If not enough data remains
                        if (res.Length < buttons.GetLength(1))
                            return ERROR_CODE.LOAD_ERROR_LINES_UNALIGNED;

                        // Set the correct row in output
                        for (int c = 0; c < buttons.GetLength(1); c++)
                            buttons[c, r].isActive = (res[c].Trim(remove) != "0");
                    }
                }

                return ERROR_CODE.ALL_OK;
            }
            catch
            {
                for (int r = 0; r < buttons.GetLength(0); r++)
                    for (int c = 0; c < buttons.GetLength(1); c++)
                        buttons[c, r].isActive = false;
                return ERROR_CODE.LOAD_WARNING_FILE_NOT_FOUND;
            }
        }

        public void GridClick(object sender, RoutedEventArgs e)
        {
            GridButton b = (GridButton)sender;
            b.isActive = !b.isActive;
        }

        public void LogMessage(String text)
        {
            LogField.Items.Add(text);
        }

        public void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveMap(buttons, CurrentMapIndex);

            // The dimensions of current matrix
            int rows = buttons.GetLength(0);
            int cols = buttons.GetLength(1);

            // These are the acceptable sizes for the files on disk
            int correctLength = 1 + 2 * rows + 3 * cols * rows;
            int correctRows = 2 +  rows;

            // Buffer vars
            String os = "";
            int readMaps = maps;

            for (int m = 0; m < maps; m++)
            {
                try
                {
                    string text = System.IO.File.ReadAllText(@"Maps/Map - " + m.ToString() + ".h");

                    int start = text.IndexOf("{");
                    int stop = text.IndexOf("};")+1;
                    text = text.Substring(start, stop - start);

                    if (text.Length != correctLength)
                        throw new Exception("Bad amount of data");

                    if (text.Split('\n').Length != correctRows)
                        throw new Exception("Bad amount of lines");

                    os += text + ",\n";
                }
                catch
                {
                    // If something fails remove this file
                    readMaps--;
                }
            }
            os = "bool maps[" + readMaps.ToString() + "][" + (rows * cols).ToString() + "] = {\n" + os.Remove(os.Length - 2) + "};";

            System.IO.File.WriteAllText(@"LEDMapping.h", os);
            LogMessage("File saved as: LEDMapping.h");
        }

        private void MatrixSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CurrentMapIndex != -1)
                SaveMap(buttons, CurrentMapIndex);

            ERROR_CODE res = LoadMap(buttons, MatrixSelection.SelectedIndex);

            if (res > ERROR_CODE.LOAD_WARNING_FILE_NOT_FOUND)
                LogMessage(res.ToString());

            CurrentMapIndex = MatrixSelection.SelectedIndex;
        }

        private void SetZero_Click(object sender, RoutedEventArgs e)
        {
            for (int r = 0; r < buttons.GetLength(0); r++)
                for (int c = 0; c < buttons.GetLength(1); c++)
                    buttons[c, r].isActive = false;
        }

        private void SetOne_Click(object sender, RoutedEventArgs e)
        {
            for (int r = 0; r < buttons.GetLength(0); r++)
                for (int c = 0; c < buttons.GetLength(1); c++)
                    buttons[c, r].isActive = true;
        }

        private void InvertAll_Click(object sender, RoutedEventArgs e)
        {
            for (int r = 0; r < buttons.GetLength(0); r++)
                for (int c = 0; c < buttons.GetLength(1); c++)
                    buttons[c, r].isActive = !buttons[c, r].isActive;
        }

        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            ERROR_CODE res = LoadMap(buttons, MatrixSelection.SelectedIndex);
            if (res > ERROR_CODE.LOAD_WARNING_FILE_NOT_FOUND)
                LogMessage(res.ToString());
        }

    }
}
