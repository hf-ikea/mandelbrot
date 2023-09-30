using System;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Runtime.InteropServices;

// https://en.wikipedia.org/wiki/Plotting_algorithms_for_the_Mandelbrot_set
// Manually simulates a complex number data type using two seperate variables...

public class MandelbrotSet
{
    public const int sizeX = 5000;
    public const int sizeY = 5000;
    public const int maxIteration = 400;
    public const bool smooth = true;

    //                              translation ▼
    public readonly static float xTranslation = 0.65f * sizeX;
    public readonly static float yTranslation = 0.50f * sizeX;

    public static int[,]? iterationCounts;

    public static double MandelbrotIteration(int pixX, int pixY, bool smooth)
    {
        //          translation ▼   size divisor ▼
        double x0 = (pixX - xTranslation) * 2.2;
        double y0 = (pixY - yTranslation) * 2.2;
        x0 /= sizeX;
        y0 /= sizeY;
        double x = 0;
        double y = 0;
        double i = 0;
        double x2 = 0;
        double y2 = 0;

        // run until escape, save iteration count in array
        while((x2 + y2) <= 4 && i < maxIteration)
        {
            y = 2.0 * x * y + y0;
            x = x2 - y2 + x0;        // x goes after y to prevent wack iterations
            x2 = x * x;
            y2 = y * y;
            i++;
        }

        if(smooth)
        {
            if(i < maxIteration)
            {
                double log_zn = Math.Log(x2 + y2) / 2.0;
                double nu = Math.Log(log_zn / Math.Log(2.0)) / Math.Log(2.0);
                i = i + 1.0 - nu;
            }
        }

        return i;
    }

    public static void RenderLine(int lineNum, ref int[,] iterationCounts, List<Color> palette, ref int[] imageBits, bool smooth)
    {
        for(int pixX = 0; pixX < sizeX; pixX++)
        {
            double i = MandelbrotIteration(pixX, lineNum, smooth);
            if(smooth)
            {
                if(i < maxIteration)
                {
                    Color color1 = Palette((i - 1.0) / maxIteration);
                    Color color2 = Palette( i        / maxIteration);
                    
                    imageBits[pixX + (lineNum * sizeX)] = LinearInterpolateColor(color1, color2, i % 1.0).ToArgb();
                }
                else
                {
                    imageBits[pixX + (lineNum * sizeX)] = unchecked((int)0xFF000000);
                }
            }
            else
            {
                imageBits[pixX + (lineNum * sizeX)] = Palette(i / maxIteration).ToArgb();
            }

            //iterationCounts[pixX, lineNum] = i;
        }
    }
    public static void Main()
    {
        iterationCounts = new int[sizeX, sizeY];
        int[] numIterationsPerPixel = new int[maxIteration + 1];
        double[,] hue = new double[sizeX, sizeY];
        Color[,] image = new Color[sizeX, sizeY];

        List<Color> palette = GetGradients(Color.FromArgb(255, 170, 23), Color.FromArgb(0, 0, 0), maxIteration + 1); // create linear pallete, works okay

        // https://stackoverflow.com/questions/59454394/how-to-create-and-write-an-image-from-a-2d-array
        int[] imageBits = new int[sizeX * sizeY];
        GCHandle handle = GCHandle.Alloc(imageBits, GCHandleType.Pinned);
        Bitmap bmp = new Bitmap(sizeX, sizeY, sizeX * 4, PixelFormat.Format32bppPArgb, handle.AddrOfPinnedObject());

        Console.WriteLine("Start escape algorithm");

        // loop through every line
        Parallel.For(0, sizeY, line => {
            RenderLine(line, ref iterationCounts, palette, ref imageBits, smooth);  // Really simple parallel processing
        });
        // for(int line = 0; line < sizeY; line++)
        // {
        //     RenderLine(line, ref iterationCounts, ref image, palette);  // Single thread
        // }

        Console.WriteLine("Finish escape algorithm");

        // // Start histogram coloring
        // for(int x = 0; x < sizeX; x++)
        // {
        //     for(int y = 0; y < sizeY; y++)
        //     {
        //         int i = iterationCounts[x, y];
        //         numIterationsPerPixel[i]++;
        //     }
        // }

        // // calculate totals for normalization
        // double total = 0;
        // for(int i = 0; i < maxIteration; i++)
        // {
        //     total += numIterationsPerPixel[i];
        // }

        // for(int x = 0; x < sizeX; x++)
        // {
        //     for(int y = 0; y < sizeY; y++)
        //     {
        //         int iteration = iterationCounts[x, y];
        //         for(int i = 0; i < iteration; i++)
        //         {
        //             hue[x, y] += numIterationsPerPixel[i] / total;
        //         }
        //     }
        // }

        // // finish histogram, start coloring bitmap
        // for(int x = 0; x < sizeX; x++)
        // {
        //     for(int y = 0; y < sizeY; y++)
        //     {
        //         bmp.SetPixel(x, y, palette[(int)Math.Round(Math.Pow(hue[x, y], 5) * (maxIteration - 1))]); // exponetial coloring is more pleasing
        //     }
        // } 

        bmp.Save("out.png", ImageFormat.Png);

        Console.WriteLine("Done!");
    }

    public static void SetPixelColor(int x, int y, Color color, ref int[] imageBits)
    {
        int index = x + (y * sizeX);
        imageBits[index] = color.ToArgb();
    }

    public static Color Palette(double percent)
    { 
        double redPercent = Math.Min(2 - (percent * 2), 1);
        double greenPercent = Math.Min(percent * 2, 1);

        double red = 255f * redPercent;
        double green = 255f * greenPercent;

        return Color.FromArgb((int)red, (int)green, 0);
    }

    public static Color LinearInterpolateColor(Color color1, Color color2, double percent)
    {
        Color color = Color.FromArgb(
            (int)(color1.R * (1.0 - percent) + color2.R * percent),
            (int)(color1.G * (1.0 - percent) + color2.G * percent),
            (int)(color1.B * (1.0 - percent) + color2.B * percent)
        );

        return color;
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