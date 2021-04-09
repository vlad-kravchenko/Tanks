using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Tanks
{
    public partial class MainWindow : Window
    {
        Random rand = new Random();
        int cols = 25, rows = 25, walls = 10, enemiesCount = 5;
        Image UserImage;
        DispatcherTimer mainTimer, bulletTimer;
        Direction look = Direction.UP;
        List<Bullet> bullets = new List<Bullet>();
        List<Enemy> enemies = new List<Enemy>();

        public MainWindow()
        {
            InitializeComponent();
            Start_Click(null, null);
        }

        private void mainTimerTick(object sender, EventArgs e)
        {
            foreach (var enemy in enemies)
            {
                Image enemyImage = GetEnemyImage(enemy);
                if (enemyImage == null) continue;

                if (!MoveEnemy(enemy, enemyImage, enemy.PrevDir))
                {
                    var newDir = (Direction)rand.Next(1, 5);
                    MoveEnemy(enemy, enemyImage, newDir);
                }

                Direction dirLook = CanSeeUser(enemy);
                if (dirLook != Direction.NONE)
                {
                    RotateImage(enemyImage, dirLook);
                    enemy.PrevDir = dirLook;
                    if (rand.Next(1, 3) == 2)
                        Shoot(Grid.GetRow(enemyImage), Grid.GetColumn(enemyImage), dirLook, false);
                }
            }
            foreach (var bullet in bullets)
                CheckIfHit(bullet);
        }

        private Image GetEnemyImage(Enemy enemy)
        {
            return MainGrid.Children.Cast<Image>()
                .FirstOrDefault(i => Grid.GetRow(i) == enemy.Row && Grid.GetColumn(i) == enemy.Col && (CellType)i.Tag == CellType.Enemy);
        }

        private bool MoveEnemy(Enemy enemy, Image img, Direction dir)
        {
            int row = enemy.Row;
            int col = enemy.Col;
            switch (dir)
            {
                case Direction.LEFT:
                    if (InRange(row, col - 1) && CanMoveEnemy(row, col - 1))
                        return MoveEnemy(enemy, img, row, col - 1, dir);
                    break;
                case Direction.RIGHT:
                    if (InRange(row, col + 1) && CanMoveEnemy(row, col + 1))
                        return MoveEnemy(enemy, img, row, col + 1, dir);
                    break;
                case Direction.DOWN:
                    if (InRange(row + 1, col) && CanMoveEnemy(row + 1, col))
                        return MoveEnemy(enemy, img, row + 1, col, dir);
                    break;
                case Direction.UP:
                    if (InRange(row - 1, col) && CanMoveEnemy(row - 1, col))
                        return MoveEnemy(enemy, img, row - 1, col, dir);
                    break;
            }
            return false;
        }

        private bool MoveEnemy(Enemy enemy, Image img, int row, int col, Direction dir)
        {
            Grid.SetRow(img, row);
            Grid.SetColumn(img, col);
            enemy.Row = row;
            enemy.Col = col;
            RotateImage(img, dir);
            enemy.PrevDir = dir;
            return true;
        }

        private Direction CanSeeUser(Enemy enemy)
        {
            for (int row = enemy.Row; row < rows; row++)
            {
                if (FacedWall(enemy, row, true)) break;
                if (FacedUser(enemy, row, true)) return Direction.DOWN;
            }
            for (int row = enemy.Row; row > -1; row--)
            {
                if (FacedWall(enemy, row, true)) break;
                if (FacedUser(enemy, row, true)) return Direction.UP;
            }
            for (int col = enemy.Col; col < cols; col++)
            {
                if (FacedWall(enemy, col, false)) break;
                if (FacedUser(enemy, col, false)) return Direction.RIGHT;
            }
            for (int col = enemy.Col; col > -1; col--)
            {
                if (FacedWall(enemy, col, false)) break;
                if (FacedUser(enemy, col, false)) return Direction.LEFT;
            }
            return Direction.NONE;
        }

        private bool FacedWall(Enemy enemy, int level, bool row)
        {
            if (row)
                return MainGrid.Children.Cast<Image>().Any(c => (CellType)c.Tag == CellType.Wall && Grid.GetRow(c) == level && Grid.GetColumn(c) == enemy.Col);
            else
                return MainGrid.Children.Cast<Image>().Any(c => (CellType)c.Tag == CellType.Wall && Grid.GetColumn(c) == level && Grid.GetRow(c) == enemy.Row);
        }

        private bool FacedUser(Enemy enemy, int level, bool row)
        {
            if (row)
                return MainGrid.Children.Cast<Image>().Any(c => (CellType)c.Tag == CellType.User && Grid.GetRow(c) == level && Grid.GetColumn(c) == enemy.Col);
            else
                return MainGrid.Children.Cast<Image>().Any(c => (CellType)c.Tag == CellType.User && Grid.GetColumn(c) == level && Grid.GetRow(c) == enemy.Row);
        }

        private void bulletTimerTick(object sender, EventArgs e)
        {
            var shots = MainGrid.Children.Cast<Image>().Where(i => (CellType)i.Tag == CellType.Bullet).ToList();
            for (int i = 0; i < shots.Count; i++) MainGrid.Children.Remove(shots[i]);

            foreach (var bullet in bullets)
            {
                if (Grid.GetColumn(UserImage) == bullet.Col && Grid.GetRow(UserImage) == bullet.Row && !bullet.FriendlyFire)
                {
                    MessageBox.Show("You lost :(");
                    Start_Click(null, null);
                    return;
                }

                switch (bullet.Dir)
                {
                    case Direction.LEFT:
                        if (InRange(bullet.Row, bullet.Col - 1) && CanMoveBullet(bullet.Row, bullet.Col - 1))
                            PlaceBullet(bullet, bullet.Row, bullet.Col - 1);
                        else
                            bullet.ToRemove = true;
                        break;
                    case Direction.RIGHT:
                        if (InRange(bullet.Row, bullet.Col + 1) && CanMoveBullet(bullet.Row, bullet.Col + 1))
                            PlaceBullet(bullet, bullet.Row, bullet.Col + 1);
                        else
                            bullet.ToRemove = true;
                        break;
                    case Direction.DOWN:
                        if (InRange(bullet.Row + 1, bullet.Col) && CanMoveBullet(bullet.Row + 1, bullet.Col))
                            PlaceBullet(bullet, bullet.Row + 1, bullet.Col);
                        else
                            bullet.ToRemove = true;
                        break;
                    case Direction.UP:
                        if (InRange(bullet.Row - 1, bullet.Col) && CanMoveBullet(bullet.Row - 1, bullet.Col))
                            PlaceBullet(bullet, bullet.Row - 1, bullet.Col);
                        else
                            bullet.ToRemove = true;
                        break;
                }
            }
            bullets.RemoveAll(b => b.ToRemove);
        }

        private void PlaceBullet(Bullet bullet, int row, int col)
        {
            Image image = CreateImage(CellType.Bullet);
            MainGrid.Children.Add(image);
            Grid.SetRow(image, row);
            Grid.SetColumn(image, col);
            bullet.Row = row;
            bullet.Col = col;
            CheckIfHit(bullet);
        }

        private Image CreateImage(CellType cellType)
        {
            return new Image
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Stretch = Stretch.Fill,
                Source = Resources[cellType.ToString()] as BitmapImage,
                Tag = cellType
            };
        }

        private void CheckIfHit(Bullet bullet)
        {
            Image enemy = MainGrid.Children.Cast<Image>().FirstOrDefault(i => Grid.GetRow(i) == bullet.Row
                                                    && Grid.GetColumn(i) == bullet.Col
                                                    && (CellType)i.Tag == CellType.Enemy);
            if (enemy != null && bullet.FriendlyFire == true)
            {
                bullet.ToRemove = true;
                MainGrid.Children.Remove(enemy);
                enemies.RemoveAll(en => en.Row == bullet.Row && en.Col == bullet.Col);
            }
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button) (sender as Button).Focusable = false;

            if (mainTimer != null) mainTimer.Stop();
            mainTimer = new DispatcherTimer();
            mainTimer.Tick += new EventHandler(mainTimerTick);
            mainTimer.Interval = new TimeSpan(0, 0, 0, 0, 600);
            mainTimer.Start();

            if (bulletTimer != null) bulletTimer.Stop();
            bulletTimer = new DispatcherTimer();
            bulletTimer.Tick += new EventHandler(bulletTimerTick);
            bulletTimer.Interval = new TimeSpan(0, 0, 0, 0, 200);
            bulletTimer.Start();

            look = Direction.UP;
            bullets.Clear();
            enemies.Clear();
            MainGrid.Children.Clear();
            MainGrid.RowDefinitions.Clear();
            MainGrid.ColumnDefinitions.Clear();
            for (int i = 0; i < cols; i++)
            {
                MainGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }
            for (int i = 0; i < rows; i++)
            {
                MainGrid.RowDefinitions.Add(new RowDefinition());
            }
            int counter = 0;
            while (counter < walls)
            {
                int row = rand.Next(0, rows);
                int col = rand.Next(0, cols);
                if (MainGrid.Children.Cast<Image>().Count(i => Grid.GetRow(i) == row && Grid.GetColumn(i) == col) > 0) continue;

                var image = CreateImage(CellType.Wall);
                MainGrid.Children.Add(image);
                Grid.SetRow(image, row);
                Grid.SetColumn(image, col);
                counter++;
            }
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    if (MainGrid.Children.Cast<Image>().Count(i => Grid.GetRow(i) == row && Grid.GetColumn(i) == col) > 0) continue;
                    var image = CreateImage(CellType.Grass);
                    MainGrid.Children.Add(image);
                    Grid.SetRow(image, row);
                    Grid.SetColumn(image, col);
                }
            }

            while (true)
            {
                int row = rand.Next(0, rows);
                int col = rand.Next(0, cols);
                if (!Empty(row, col)) continue;

                UserImage = CreateImage(CellType.User);
                MainGrid.Children.Add(UserImage);
                Grid.SetRow(UserImage, row);
                Grid.SetColumn(UserImage, col);
                break;
            }

            counter = 0;
            while (counter < enemiesCount)
            {
                int row = rand.Next(0, rows);
                int col = rand.Next(0, cols);
                if (!Empty(row, col)) continue;

                Enemy enemy = new Enemy
                {
                    Row = row,
                    Col = col,
                    Dir = Direction.UP,
                    PrevDir = Direction.UP
                };

                var image = CreateImage(CellType.Enemy);
                enemies.Add(enemy);
                MainGrid.Children.Add(image);
                Grid.SetRow(image, row);
                Grid.SetColumn(image, col);
                counter++;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            int row = Grid.GetRow(UserImage);
            int col = Grid.GetColumn(UserImage);
            if (!InRange(row, col)) return;

            switch (e.Key)
            {
                case Key.W:
                    RotateImage(UserImage, Direction.UP);
                    if (InRange(row - 1, col) && CanMoveUser(row - 1, col)) Grid.SetRow(UserImage, row - 1);
                    break;
                case Key.S:
                    RotateImage(UserImage, Direction.DOWN);
                    if (InRange(row + 1, col) && CanMoveUser(row + 1, col)) Grid.SetRow(UserImage, row + 1);
                    break;
                case Key.A:
                    RotateImage(UserImage, Direction.LEFT);
                    if (InRange(row, col - 1) && CanMoveUser(row, col - 1)) Grid.SetColumn(UserImage, col - 1);
                    break;
                case Key.D:
                    RotateImage(UserImage, Direction.RIGHT);
                    if (InRange(row, col + 1) && CanMoveUser(row, col + 1)) Grid.SetColumn(UserImage, col + 1);
                    break;
                case Key.Space:
                    Shoot(row, col, look, true);
                    break;
            }
        }

        private void Shoot(int row, int col, Direction direction, bool user)
        {
            bullets.Add(new Bullet
            {
                Row = row,
                Col = col,
                Dir = direction,
                ToRemove = false,
                FriendlyFire = user
            });
        }

        private void RotateImage(Image img, Direction dir)
        {
            BitmapImage bmp = new BitmapImage();
            if (img == UserImage)
            {
                bmp = Resources["User"] as BitmapImage;
                look = dir;
            }
            else
            {
                bmp = Resources["Enemy"] as BitmapImage;
            }

            TransformedBitmap tBmp = new TransformedBitmap();
            tBmp.BeginInit();
            tBmp.Source = bmp;
            RotateTransform rt = new RotateTransform(0);
            switch (dir)
            {
                case Direction.LEFT:
                    rt = new RotateTransform(270);
                    break;
                case Direction.RIGHT:
                    rt = new RotateTransform(90);
                    break;
                case Direction.DOWN:
                    rt = new RotateTransform(180);
                    break;
                case Direction.UP:
                    rt = new RotateTransform(0);
                    break;
            }
            tBmp.Transform = rt;
            tBmp.EndInit();
            img.Source = tBmp;
        }

        private bool InRange(int row, int col)
        {
            return row > -1 && row < rows && col > -1 && col < cols;
        }

        private bool Empty(int row, int col)
        {
            var children = MainGrid.Children
              .Cast<Image>()
              .Where(i => Grid.GetRow(i) == row && Grid.GetColumn(i) == col);
            children = children.Where(i => (CellType)i.Tag != CellType.Grass && (CellType)i.Tag != CellType.Wall);
            return children.Count() == 0;
        }

        private bool CanMoveUser(int row, int col)
        {
            var children = MainGrid.Children
                          .Cast<Image>()
                          .Where(i => Grid.GetRow(i) == row && Grid.GetColumn(i) == col);
            return !children.Any(i => (CellType)i.Tag == CellType.Wall || (CellType)i.Tag == CellType.Enemy);
        }

        private bool CanMoveBullet(int row, int col)
        {
            var children = MainGrid.Children
              .Cast<Image>()
              .Where(i => Grid.GetRow(i) == row && Grid.GetColumn(i) == col);
            return !children.Any(i => (CellType)i.Tag == CellType.Wall);
        }

        private bool CanMoveEnemy(int row, int col)
        {
            var children = MainGrid.Children
              .Cast<Image>()
              .Where(i => Grid.GetRow(i) == row && Grid.GetColumn(i) == col);
            return !children.Any(i => (CellType)i.Tag == CellType.Wall || (CellType)i.Tag == CellType.Enemy || (CellType)i.Tag == CellType.User);
        }
    }
}