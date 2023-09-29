using System;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;

// https://en.wikipedia.org/wiki/Plotting_algorithms_for_the_Mandelbrot_set
// Manually simulates a complex number data type using two seperate variables...

public class MandelbrotSet
{
    public readonly static int sizeX = 5000;
    public readonly static int sizeY = 5000;
    public readonly static int maxIteration = 200;

    public static int[,]? iterationCounts;

    public static void RenderLine(int LineNum, ref int[,] iterationCounts)
    {
        for(int pixX = 0; pixX < sizeX; pixX++)
        {
            //       translation ▼   size divisor ▼
            double x0 = (pixX - (0.65 * sizeX)) * 3;
            double y0 = (LineNum - (0.5  * sizeY)) * 3;
            x0 /= sizeX;
            y0 /= sizeY;
            double x = 0;
            double y = 0;
            int i = 0;
            double x2 = 0;
            double y2 = 0;

            // run until escape, save iteration count in array
            while((x2 + y2) <= 4 && i < maxIteration)
            {
                y = 2 * x * y + y0;
                x = x2 - y2 + x0;        // x goes after y to prevent wack iterations
                x2 = x * x;
                y2 = y * y;
                i++;
            }

            iterationCounts[pixX, LineNum] = i;
        }
    }
    public static void Main()
    {
        iterationCounts = new int[sizeX, sizeY];
        int[] numIterationsPerPixel = new int[maxIteration + 1];
        double[,] hue = new double[sizeX, sizeY];

        List<Color> palette = GetGradients(Color.FromArgb(0, 57, 143), Color.FromArgb(0, 0, 0), maxIteration); // create linear pallete, works okay
        Bitmap bmp = new Bitmap(sizeX, sizeY);

        Console.WriteLine("Start escape algorithm");

        // loop through every line
        Parallel.For(0, sizeY, line => {
            RenderLine(line, ref iterationCounts);  // Parallel
        });

        // for(int line = 0; line < sizeY; line++)
        // {
        //     RenderLine(line, ref iterationCounts);  // Single thread
        // }
        

        Console.WriteLine("Finish escape algorithm\nStart Histogram");

        // Start histogram coloring
        for(int x = 0; x < sizeX; x++)
        {
            for(int y = 0; y < sizeY; y++)
            {
                int i = iterationCounts[x, y];
                numIterationsPerPixel[i]++;
            }
        }

        // calculate totals for normalization
        double total = 0;
        for(int i = 0; i < maxIteration; i++)
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
                bmp.SetPixel(x, y, palette[(int)Math.Round(Math.Pow(hue[x, y], 5) * (maxIteration - 1))]); // exponetial coloring is more pleasing
            }
        } 

        bmp.Save("out.png", ImageFormat.Png);

        // done!
    }

    public static List<Color> GetGradients(Color start, Color end, int steps)
    {
        List<Color> gradients = new List<Color>();
        float stepR = (end.R - start.R) / (float)steps;
        float stepG = (end.G - start.G) / (float)steps;
        float stepB = (end.B - start.B) / (float)steps;

        for (int i = 0; i < steps; i++)
        {
            gradients.Add(Color.FromArgb((int)(start.R + (stepR * i)),
                                         (int)(start.G + (stepG * i)),
                                         (int)(start.B + (stepB * i))));
        }

        return gradients;
    }
}