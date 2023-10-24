using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace CrystalX
{
    public class Pixel
    {
        public Point Position { get; set; }
        public Color color { get; set; }

        public Pixel(Point Position, Color color)
        {
            this.Position = Position;
            this.color = color;
        }
    }

    class Label
    {
        public int Name { get; set; }
        public Label Root { get; set; }
        public int Rank { get; set; }

        public Label(int Name)
        {
            this.Name = Name;
            this.Root = this;
            this.Rank = 0;
        }

        public Label GetRoot()
        {
            if (this.Root != this)
            {
                this.Root = this.Root.GetRoot();
            }
            return this.Root;
        }

        public void Join(Label root2)
        {
            if (root2.Rank < this.Rank)//is the rank of Root2 less than that of Root1 ?
            {
                root2.Root = this;//yes! then Root1 is the parent of Root2 (since it has the higher rank)
            }
            else //rank of Root2 is greater than or equal to that of Root1
            {
                this.Root = root2;//make Root2 the parent
                if (this.Rank == root2.Rank)//both ranks are equal ?
                {
                    root2.Rank++;//increment Root2, we need to reach a single root for the whole tree
                }
            }
        }
    }

    public class Blob
    {
        public Bitmap image {get; set;}
        public Point CG { get; set; }
        public double Fullness { get; set; }
        public double Area { get; set; }

        public Blob(Bitmap _img){
            image = _img;
        }
    }

    public class ConnectedComponentLabeling
    {
        private int[,] _board;
        private Bitmap _input;
        private int _width;
        private int _height;

        public IDictionary<int, Blob> Process(Bitmap input)
        {
            _input = input;
            _width = input.Width;
            _height = input.Height;
            _board = new int[_width, _height];

            Dictionary<int, List<Pixel>> patterns = Find();
            var images = new Dictionary<int, Blob>();

            foreach (KeyValuePair<int, List<Pixel>> pattern in patterns)
            {
                Blob _blob = CreateBlob(pattern.Value);
                images.Add(pattern.Key, _blob);
            }

            return images;
        }

        private bool CheckIsBackGround(Pixel currentPixel)
        {
            bool temp = currentPixel.color.R == 0 && currentPixel.color.G == 0 && currentPixel.color.B == 0;
            return temp;
        }

        private Dictionary<int, List<Pixel>> Find()
        {
            int labelCount = 1;
            var allLabels = new Dictionary<int, Label>();

            for (int i = 0; i < _height; i++)
            {
                for (int j = 0; j < _width; j++)
                {
                    Pixel currentPixel = new Pixel(new Point(j, i), _input.GetPixel(j, i));

                    if (CheckIsBackGround(currentPixel))
                    {
                        continue;
                    }

                    IEnumerable<int> neighboringLabels = GetNeighboringLabels(currentPixel);
                    int currentLabel;

                    if (!neighboringLabels.Any())
                    {
                        currentLabel = labelCount;
                        allLabels.Add(currentLabel, new Label(currentLabel));
                        labelCount++;
                    }
                    else
                    {
                        currentLabel = neighboringLabels.Min(n => allLabels[n].GetRoot().Name);
                        Label root = allLabels[currentLabel].GetRoot();

                        foreach (var neighbor in neighboringLabels)
                        {
                            if (root.Name != allLabels[neighbor].GetRoot().Name)
                            {
                                allLabels[neighbor].Join(allLabels[currentLabel]);
                            }
                        }
                    }

                    _board[j, i] = currentLabel;
                }
            }

            Dictionary<int, List<Pixel>> patterns = AggregatePatterns(allLabels);

            return patterns;
        }

        private IEnumerable<int> GetNeighboringLabels(Pixel pix)
        {
            var neighboringLabels = new List<int>();

            for (int i = pix.Position.Y - 1; i <= pix.Position.Y + 2 && i < _height - 1; i++)
            {
                for (int j = pix.Position.X - 1; j <= pix.Position.X + 2 && j < _width - 1; j++)
                {
                    if (i > -1 && j > -1 && _board[j, i] != 0)
                    {
                        neighboringLabels.Add(_board[j, i]);
                    }
                }
            }

            return neighboringLabels;
        }

        private Dictionary<int, List<Pixel>> AggregatePatterns(Dictionary<int, Label> allLabels)
        {
            var patterns = new Dictionary<int, List<Pixel>>();

            for (int i = 0; i < _height; i++)
            {
                for (int j = 0; j < _width; j++)
                {
                    int patternNumber = _board[j, i];

                    if (patternNumber != 0)
                    {
                        patternNumber = allLabels[patternNumber].GetRoot().Name;

                        if (!patterns.ContainsKey(patternNumber))
                        {
                            patterns[patternNumber] = new List<Pixel>();
                        }

                        patterns[patternNumber].Add(new Pixel(new Point(j, i), Color.Black));
                    }
                }
            }

            return patterns;
        }

        public Blob CreateBlob(List<Pixel> pattern)
        {
            int minX = pattern.Min(p => p.Position.X);
            int maxX = pattern.Max(p => p.Position.X);

            int minY = pattern.Min(p => p.Position.Y);
            int maxY = pattern.Max(p => p.Position.Y);

            int width = maxX + 1 - minX;
            int height = maxY + 1 - minY;

            var bmp = new Bitmap(width, height);

            int sumCGx = 0;
            int sumCGy = 0;

            foreach (Pixel pix in pattern)
            {
                sumCGx += pix.Position.X - minX;
                sumCGy += pix.Position.Y - minY;
                bmp.SetPixel(pix.Position.X - minX, pix.Position.Y - minY, pix.color);//shift position by minX and minY
            }

            Blob _blob = new Blob(bmp);
            _blob.CG = new Point(sumCGx / pattern.Count, sumCGy / pattern.Count);
            _blob.Area = pattern.Count; //No of foreground pixels is considered as the area of the blob
            _blob.Fullness = _blob.Area / (width * height);  
            
            return _blob;
        }
    }
}
