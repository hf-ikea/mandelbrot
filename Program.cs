using System.Drawing;
using System.Drawing.Imaging;

// Manually simulates a complex number data type using two seperate variables...

public class MandelbrotSet
{
    public static void Main()
    {
        int sizeX = 1000;
        int sizeY = 1000;
        int maxIterations = 200;

        int[,] iterationCounts = new int[sizeX, sizeY];
        int[] numIterationsPerPixel = new int[maxIterations + 1];
        double[,] hue = new double[sizeX, sizeY];

        List<Color> colorArray = GetGradients(Color.FromArgb(255, 255, 0, 0), Color.FromArgb(255, 0, 255, 255), maxIterations + 1);
        Bitmap bmp = new Bitmap(sizeX, sizeY);

        // loop through every pixel

        for(int pixX = 0; pixX < sizeX; pixX++)
        {
            for(int pixY = 0; pixY < sizeY; pixY++)
            {
                double x0 = (pixX - (0.5 * sizeX)) * 2;
                double y0 = (pixY - (0.5 * sizeY)) * 2;
                x0 /= sizeX;
                y0 /= sizeY;
                double x = 0;
                double y = 0;
                int i = 0;
                double x2 = 0;
                double y2 = 0;

                // run until escape, save iteration count in array

                while((x2 + y2) <= 4 && i < maxIterations)
                {
                    y = 2 * x * y + y0;
                    x = x2 - y2 + x0;        // x goes after y to prevent wack iterations
                    x2 = x * x;
                    y2 = y * y;
                    i++;
                }

                iterationCounts[pixX,pixY] = i;

                //Console.WriteLine("Real: " + x.ToString() + ", Imaginary: " + y.ToString());
                //bmp.SetPixel(pixX, pixY, colorArray[i]);
            }
        }

        // Start histogram coloring

        for(int x = 0; x < sizeX; x++)
        {
            for(int y = 0; y < sizeY; y++)
            {
                int i = iterationCounts[x, y];
                numIterationsPerPixel[i]++;
            }
        }

        double total = 0;
        for(int i = 0; i < maxIterations; i++)
        {
            total += numIterationsPerPixel[i];
        }

        for(int x = 0; x < sizeX; x++)
        {
            for(int y = 0; y < sizeY; y++)
            {
                int iteration = iterationCounts[x, y];
                for(int i = 0; i < iteration; i++)
                {
                    hue[x, y] += numIterationsPerPixel[i] / total;
                }
            }
        }

        // finish histogram, start coloring bitmap
        
        for(int x = 0; x < sizeX; x++)
        {
            for(int y = 0; y < sizeY; y++)
            {
                bmp.SetPixel(x, y, colorArray[(int)Math.Round(hue[x, y] * maxIterations)]);
            }
        } 

        bmp.Save("out.png", ImageFormat.Png);

        // done!
    }

    public static List<Color> GetGradients(Color start, Color end, int steps)
    {
        List<Color> gradients = new List<Color>();
        int stepA = (end.A - start.A) / (steps - 1);
        int stepR = (end.R - start.R) / (steps - 1);
        int stepG = (end.G - start.G) / (steps - 1);
        int stepB = (end.B - start.B) / (steps - 1);

        for (int i = 0; i < steps; i++)
        {
            gradients.Add(Color.FromArgb(start.A + (stepA * i),
                                         start.R + (stepR * i),
                                         start.G + (stepG * i),
                                         start.B + (stepB * i)));
        }

        return gradients;
    }
}