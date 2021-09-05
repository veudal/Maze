using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Brushes = System.Drawing.Brushes;
using Color = System.Drawing.Color;
using Pen = System.Drawing.Pen;
using Point = System.Drawing.Point;
using Timer = System.Timers.Timer;

namespace Maze
{
    public partial class MainWindow : Window
    {
        bool exceptionShowed = false;
        bool canceled = false;
        int stackoverflow = 0;
        int solvingSpeed = 0;
        int width = 12;
        int height = 12;
        int wdt;
        int hgt;
        readonly Timer timer = new Timer(3000);
        readonly Random rng = new Random();
        readonly int brushThickness = 2;
        List<Cell> pathSolution = new List<Cell>();
        Cell[,] Maze;
        Bitmap bmp;
        readonly BackgroundWorker worker = new BackgroundWorker();

        public MainWindow()
        {
            InitializeComponent();
            Prepare();
        }

        private void IntialiseControls()
        {
            if (width < 3)
            {
                width = 3;
            }
            if (height < 3)
            {
                height = 3;
            }
            if (MazeGrid.RowDefinitions.Count > 0)
            {
                MazeGrid.RowDefinitions.RemoveRange(0, MazeGrid.RowDefinitions.Count);
            }
            if (MazeGrid.ColumnDefinitions.Count > 0)
            {
                MazeGrid.ColumnDefinitions.RemoveRange(0, MazeGrid.ColumnDefinitions.Count);
            }
            for (int i = 0; i < width; i++)
            {
                MazeGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }
            for (int i = 0; i < height; i++)
            {
                MazeGrid.RowDefinitions.Add(new RowDefinition());
            }
            Grid.SetColumnSpan(MazeImage, width - 2);
            Grid.SetRowSpan(MazeImage, height);
            MazeColumns.Text = Convert.ToString(width);
            MazeRows.Text = Convert.ToString(height);
        }

        private void GenerateMaze()
        {
            ResetVariablesForGeneration();
            IntialiseControls();
            Initialise();
            CreatePassage(new Point(0, 0));
            if (exceptionShowed == false)
            {
                DrawMazeAndDisplay();
                SolveBtn.Visibility = Visibility.Visible;
            }
        }

        private void ResetVariablesForGeneration()
        {
            pathSolution = new List<Cell>();
            Maze = new Cell[height, width];
            stackoverflow = 0;
            exceptionShowed = false;
            canceled = true;
        }

        private void DrawMazeAndDisplay()
        {
            bmp = new Bitmap(Convert.ToInt32(MazeGrid.ActualWidth), Convert.ToInt32(MazeGrid.ActualHeight));
            System.Drawing.Image img = bmp;
            using (Graphics g = Graphics.FromImage(img))
            {
                System.Drawing.Rectangle ImageSize = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
                Color color = Color.FromArgb(45, 45, 48);
                SolidBrush brush = new SolidBrush(color);
                g.FillRectangle(brush, ImageSize);
                hgt = Convert.ToInt32(MazeGrid.ActualHeight) / MazeGrid.RowDefinitions.Count - 1;
                wdt = Convert.ToInt32(MazeGrid.ActualWidth) / MazeGrid.ColumnDefinitions.Count - 1;
                DrawWalls(g, new Pen(Brushes.White, brushThickness));
                DrawStart(g, Brushes.DeepSkyBlue);
            }
            ImageSource source = ConvertBitmapToBitmapImage.Convert(bmp);
            MazeImage.Source = source;
        }

        private void IllustrateSolvingProcess(Point p, System.Drawing.Brush brush)
        {
            System.Drawing.Image img = bmp;
            using (Graphics g = Graphics.FromImage(img))
            {
                DrawCurrentPos(g, brush, p);
            }
        }
        private void DrawCurrentPos(Graphics g, System.Drawing.Brush solvingBrush, Point p)
        {
            double xCoord = wdt * p.X;
            double yCoord = hgt * p.Y;
            System.Drawing.Rectangle rectangle = new System.Drawing.Rectangle(Convert.ToInt32(xCoord), Convert.ToInt32(yCoord), hgt, hgt);
            int deflateValue = -1 * (wdt / 4);
            rectangle.Inflate(deflateValue, deflateValue);
            rectangle.X += Convert.ToInt32(deflateValue * 0.75) * -1;
            g.FillEllipse(solvingBrush, rectangle);
        }

        private void Prepare()
        {
            worker.WorkerReportsProgress = true;
            worker.ProgressChanged += new ProgressChangedEventHandler(Worker_ProgressChanged);
            worker.DoWork += new DoWorkEventHandler(Worker_DoWork);
            IntialiseResources();
        }

        private void DrawWalls(Graphics g, Pen wallColor)
        {
            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    int xCurrent = wdt * col;
                    int yCurrent = hgt * row;
                    PointF p1;
                    PointF p2;
                    if (Maze[row, col].NorthWall)
                    {
                        p1 = new PointF(xCurrent, yCurrent);
                        p2 = new PointF(xCurrent + wdt, yCurrent);
                        g.DrawLine(wallColor, p1, p2);
                    }
                    if (Maze[row, col].EastWall)
                    {
                        p1 = new PointF(xCurrent + wdt, yCurrent);
                        p2 = new PointF(xCurrent + wdt, yCurrent + hgt);
                        g.DrawLine(wallColor, p1, p2);
                    }
                    if (Maze[row, col].SouthWall)
                    {
                        p1 = new PointF(xCurrent, yCurrent + hgt);
                        p2 = new PointF(xCurrent + wdt, yCurrent + hgt);
                        g.DrawLine(wallColor, p1, p2);
                    }
                    if (Maze[row, col].WestWall)
                    {
                        p1 = new PointF(xCurrent, yCurrent);
                        p2 = new PointF(xCurrent, yCurrent + hgt);
                        g.DrawLine(wallColor, p1, p2);
                    }
                    if (row == height - 1 && col == width - 1)
                    {
                        g.FillRectangle(Brushes.LimeGreen, xCurrent + 1, yCurrent + 1, wdt - 2, hgt - 2);
                    }
                }
            }
        }



        private void Initialise()
        {
            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    Maze[row, col] = new Cell();
                }
            }
        }

        private void CreatePassage(Point currentCell)
        {
            Maze[Convert.ToInt32(currentCell.Y), Convert.ToInt32(currentCell.X)].Point = new Point(currentCell.X, currentCell.Y);
            Maze[Convert.ToInt32(currentCell.Y), Convert.ToInt32(currentCell.X)].Visited = true;
            List<Direction> validDirections = GetDirections();
            ValidateDirections(currentCell, validDirections);
            if (validDirections.Count == 0)
            {
                Maze[Convert.ToInt32(currentCell.Y), Convert.ToInt32(currentCell.X)].DeadEnd = true;
            }
            while (validDirections.Count > 0 && stackoverflow < 500000)
            {
                Direction rndDirection = Direction.Invalid;
                rndDirection = SetRandomDirection(validDirections, rndDirection);
                RemoveWall(currentCell, rndDirection);
                validDirections.Remove(rndDirection);
                Point newCell = SetNewPoint(currentCell, rndDirection);
                stackoverflow++;
                if (stackoverflow > 1020)
                {
                    if (exceptionShowed == false)
                    {
                        MessageBox.Show("StackOverFlowException, die angegebene Dimension ist übermäßig.");
                        exceptionShowed = true;
                    }
                    return;
                }
                CreatePassage(newCell);
                ValidateDirections(currentCell, validDirections);
            }

        }

        private List<Direction> GetDirections()
        {
            return new List<Direction>() { Direction.North, Direction.South, Direction.West, Direction.East };
        }

        private Direction SetRandomDirection(List<Direction> validDirections, Direction rndDirection)
        {
            if (validDirections.Count > 1)
            {
                rndDirection = validDirections[rng.Next(validDirections.Count)];
            }
            else if (validDirections.Count == 1)
            {
                rndDirection = validDirections[0];
            }

            return rndDirection;
        }

        private Point SetNewPoint(Point position, Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                    position.Y--;
                    break;
                case Direction.East:
                    position.X++;
                    break;
                case Direction.South:
                    position.Y++;
                    break;
                case Direction.West:
                    position.X--;
                    break;
            }
            return position;
        }

        private void ValidateDirections(Point cellPos, List<Direction> directions)
        {
            List<Direction> invalidDirections = new List<Direction>();
            for (int i = 0; i < directions.Count; i++)
            {
                switch (directions[i])
                {
                    case Direction.North:
                        int x1 = Convert.ToInt32(cellPos.X);
                        int y1 = Convert.ToInt32(cellPos.Y) - 1;
                        if (cellPos.Y == 0 || Maze[y1, x1].Visited)
                            invalidDirections.Add(Direction.North);
                        break;
                    case Direction.East:
                        int x2 = Convert.ToInt32(cellPos.X) + 1;
                        int y2 = Convert.ToInt32(cellPos.Y);
                        if (cellPos.X == width - 1 || Maze[y2, x2].Visited)
                            invalidDirections.Add(Direction.East);
                        break;
                    case Direction.South:
                        int x3 = Convert.ToInt32(cellPos.X);
                        int y3 = Convert.ToInt32(cellPos.Y) + 1;
                        if (cellPos.Y == height - 1 || Maze[y3, x3].Visited)
                            invalidDirections.Add(Direction.South);
                        break;
                    case Direction.West:
                        int x4 = Convert.ToInt32(cellPos.X) - 1;
                        int y4 = Convert.ToInt32(cellPos.Y);
                        if (cellPos.X == 0 || Maze[y4, x4].Visited)
                            invalidDirections.Add(Direction.West);
                        break;
                }
            }

            foreach (var item in invalidDirections)
            {
                directions.Remove(item);
            }
        }

        private void RemoveWall(Point cell, Direction direction)
        {
            int y = Convert.ToInt32(cell.Y);
            int x = Convert.ToInt32(cell.X);

            switch (direction)
            {
                case Direction.North:
                    Maze[y, x].NorthWall = false;
                    Maze[y - 1, x].SouthWall = false;
                    break;
                case Direction.East:
                    Maze[y, x].EastWall = false;
                    Maze[y, x + 1].WestWall = false;
                    break;
                case Direction.South:
                    Maze[y, x].SouthWall = false;
                    Maze[y + 1, x].NorthWall = false;
                    break;
                case Direction.West:
                    Maze[y, x].WestWall = false;
                    Maze[y, x - 1].EastWall = false;
                    break;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            GenerateMaze();
        }

        private void StartVideo()
        {
            if (!File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\temp\\1d49ea4e-64d4-4bd1-9495-a7aee1304134.mp4"))
            {
                FileStream _FileStream = new FileStream(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\temp\\1d49ea4e-64d4-4bd1-9495-a7aee1304134.mp4", System.IO.FileMode.Create, System.IO.FileAccess.Write);
                _FileStream.Write(Properties.Resources.Video, 0, Properties.Resources.Video.Length);
                _FileStream.Close();
            }
            Video.Source = new Uri(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\temp\\1d49ea4e-64d4-4bd1-9495-a7aee1304134.mp4");
            Video.Position = TimeSpan.FromSeconds(1);
            Video.Play();
            Video.Visibility = Visibility.Visible;
            timer.Elapsed += ShowBtn;
            timer.Start();
        }

        private void ShowBtn(object sender, System.Timers.ElapsedEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)(() => end.Visibility = Visibility.Visible));
            timer.Stop();
        }

        private void DrawStart(Graphics g, System.Drawing.Brush startBrush)
        {
            g.FillRectangle(startBrush, 1, 1, wdt - 2, hgt - 2);
        }

        private void ValidationTextBox(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            int max = 99;
            e.Handled = !(int.TryParse(((TextBox)sender).Text + e.Text, out int i) && i >= 1 && i <= max);
        }

        private void MazeColumns_TextChanged(object sender, TextChangedEventArgs e)
        {
            int max = 99;

            if (!int.TryParse(((TextBox)sender).Text, out int j) || j < 1 || j > max)
            {
                ((TextBox)sender).Text = "";
            }
            else
            {
                ((TextBox)sender).Text = j.ToString();
                width = j;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Video.Stop();
            Video.Visibility = Visibility.Collapsed;
            end.Visibility = Visibility.Collapsed;
        }

        private void MazeRows_TextChanged(object sender, TextChangedEventArgs e)
        {
            int max = 99;

            if (!int.TryParse(((TextBox)sender).Text, out int j) || j < 1 || j > max)
            {
                ((TextBox)sender).Text = "";
            }
            else
            {
                ((TextBox)sender).Text = j.ToString();
                height = j;
            }
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            StartVideo();
        }

        private void SolveBtn_Click(object sender, RoutedEventArgs e)
        {
            SolveMaze();
        }

        private bool FloodFill(Cell cell)
        {
            if (stackoverflow < 1020)
            {
                List<Cell> neighbors = new List<Cell>();
                stackoverflow++;
                if (cell.DeadEnd == true)
                {
                    return false;
                }
                int count = 0;
                DetectValidNeighbors(cell, neighbors);
                foreach (Cell n in neighbors.ToArray())
                {
                    if (n.DeadEnd == false && n.Visited == false)
                    {
                        n.Visited = true;
                        count++;
                        if (n.Point == new Point(width - 1, height - 1))
                        {
                            pathSolution.Add(cell);
                            return true;
                        }
                        bool reachesEnd = FloodFill(n);
                        if (reachesEnd == true)
                        {
                            pathSolution.Add(cell);
                            return true;
                        }
                    }
                }
                if (cell.Point != new Point(width - 1, height - 1))
                {
                    if (count == 1)
                    {
                        cell.DeadEnd = true;
                    }
                }
                else
                {
                    return true;
                }
            }
            else
            {
                MessageBox.Show(stackoverflow.ToString());
            }
            return false;
        }

        private void DetectValidNeighbors(Cell cell, List<Cell> neighbors)
        {
            if (cell.NorthWall == false && cell.Point.Y > 0)
            {
                neighbors.Add(Maze[cell.Point.Y - 1, cell.Point.X]);
            }
            if (cell.SouthWall == false && cell.Point.Y < height)
            {
                neighbors.Add(Maze[cell.Point.Y + 1, cell.Point.X]);
            }
            if (cell.EastWall == false && cell.Point.X < width)
            {
                neighbors.Add(Maze[cell.Point.Y, cell.Point.X + 1]);
            }
            if (cell.WestWall == false && cell.Point.X > 0)
            {
                neighbors.Add(Maze[cell.Point.Y, cell.Point.X - 1]);
            }
        }

        private static void IntialiseResources()
        {
            if (!File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\temp\\1d49ea4e-64d4-4bd1-9495-a7aee1304134.mp4"))
            {
                FileStream _FileStream = new FileStream(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\temp\\1d49ea4e-64d4-4bd1-9495-a7aee1304134.mp4", System.IO.FileMode.Create, System.IO.FileAccess.Write);
                _FileStream.Write(Properties.Resources.Video, 0, Properties.Resources.Video.Length);
                _FileStream.Close();
            }
        }

        private void SolveMaze()
        {
            ResetVariablesForSolve();
            FloodFill(Maze[0, 0]);
            DrawPath();
        }

        private void DrawPath()
        {
            try
            {
                this.worker.RunWorkerAsync();
            }
            catch
            {

            }
        }

        private void ResetVariablesForSolve()
        {
            foreach (var c in Maze)
            {
                c.Visited = false;
                c.DeadEnd = false;
            }
            canceled = false;
            stackoverflow = 0;
        }


        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            int speed = 1;
            if (solvingSpeed == 1)
            {
                speed = 2;
            }
            else if (solvingSpeed == 2)
            {
                speed = 3;
            }
            pathSolution.Reverse();
            hgt = Convert.ToInt32(MazeGrid.ActualHeight) / MazeGrid.RowDefinitions.Count - 1;
            wdt = Convert.ToInt32(MazeGrid.ActualWidth) / MazeGrid.ColumnDefinitions.Count - 1;
            if (speed != 3)
            {
                int i = 1;
                BackgroundWorker bkgWorker = (BackgroundWorker)sender;
                foreach (var p in pathSolution.ToArray())
                {
                    try
                    {
                        if (canceled == false)
                        {
                            if (p.DeadEnd == false)
                            {
                                if (p.Point != new Point(0, 0) && p.Point != new Point(width - 1, height - 1))
                                {
                                    i++;
                                    IllustrateSolvingProcess(p.Point, Brushes.LightSkyBlue);
                                    Dispatcher.BeginInvoke((Action)(() =>
                                    {
                                        MazeImage.Source = ConvertBitmapToBitmapImage.Convert(bmp);
                                    }));
                                    if (solvingSpeed == 1)
                                    {
                                        Thread.Sleep(40);
                                    }
                                    else if (solvingSpeed == 0)
                                    {
                                        Thread.Sleep(100);
                                    }
                                    else
                                    {
                                        InstantSolvingIllustration();
                                        break;
                                    }
                                    bkgWorker.ReportProgress(i);
                                }
                            }
                        }
                    }
                    catch
                    {

                    }

                }
            }
            else
            {
                InstantSolvingIllustration();
            }
        }

        private void InstantSolvingIllustration()
        {
            foreach (var p in pathSolution.ToArray())
            {
                if (p.DeadEnd == false)
                {
                    if (p.Point != new Point(0, 0) && p.Point != new Point(width - 1, height - 1))
                    {
                        IllustrateSolvingProcess(p.Point, Brushes.LightSkyBlue);
                    }
                }

            }
            Dispatcher.BeginInvoke((Action)(() =>
            {
                MazeImage.Source = ConvertBitmapToBitmapImage.Convert(bmp);
            }));
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            UpdateLayout();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            solvingSpeed = Convert.ToInt32(e.NewValue);
            SpeedSlider.Value = Convert.ToInt32(e.NewValue);
        }
    }
}
