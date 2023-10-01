#pragma warning disable CS0162

using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

// https://en.wikipedia.org/wiki/Plotting_algorithms_for_the_Mandelbrot_set
// Manually simulates a complex number data type using two seperate variables...

public class MandelbrotSet
{
    public const int sizeX = 10000;
    public const int sizeY = 10000;
    public const int maxIteration = 512;
    public const bool smooth = false;
    public const bool histogram = true; // both cannot be true

    //                              translation ▼
    public readonly static float xTranslation = 0.65f * sizeX;
    public readonly static float yTranslation = 0.50f * sizeX;

    public static int[,]? iterationCounts = new int[sizeX, sizeY];

    public static int[] imageBits = new int[sizeX * sizeY];

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

    public static void RenderLine(int lineNum, ref int[,] iterationCounts, Color[] palette, bool smooth)
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

                    SetPixelColor(pixX, lineNum, LinearInterpolateColor(color1, color2, i % 1.0));
                }
                else
                {
                    SetPixelColor(pixX, lineNum, Color.Black);
                }
            }
            else if(histogram)
            {
                iterationCounts[pixX, lineNum] = (int)i;
            }
            else
            {
                SetPixelColor(pixX, lineNum, palette[(pixX + lineNum) % palette.Length]);
            }
        }
    }
    public static void Main()
    {
        Color[] colors = new Color[] { Color.Blue, Color.Red, Color.Lime};
        Color[] palette = GenerateGradient(colors, maxIteration + 2); // create linear palette, works okay
        iterationCounts = new int[sizeX, sizeY];
        int count = 0;
        object countLock = new();

        ProgressBar progress = new();

        // https://stackoverflow.com/questions/59454394/how-to-create-and-write-an-image-from-a-2d-array
        imageBits = new int[sizeX * sizeY];
        GCHandle handle = GCHandle.Alloc(imageBits, GCHandleType.Pinned);
        Bitmap bmp = new(sizeX, sizeY, sizeX * 4, PixelFormat.Format32bppPArgb, handle.AddrOfPinnedObject());

        Console.Write("Escaping...");

        // loop through every line
        Parallel.For(0, sizeY, line => {
            RenderLine(line, ref iterationCounts, palette, smooth);  // Really simple parallel processing
            lock(countLock) { UpdateProgressBar(++count, sizeY, progress); }
        });
        Console.WriteLine("Done.");

        Console.WriteLine("Finish escape algorithm");

        if(histogram && !smooth)
        {
            HistogramColoring(iterationCounts, palette);
        }

        bmp.Save("out.png", ImageFormat.Png);

        Console.WriteLine("Done!");
    }

    public static void UpdateProgressBar(int progress, int total, ProgressBar progressBar)
    {
        progressBar.Report((float)progress / total);
    }

    public static void HistogramColoring(int[,] iterationCounts, Color[] palette)
    {
        Console.WriteLine("Start Histogram Coloring");
        int[] numIterationsPerPixel = new int[maxIteration + 1];
        double[,] hue = new double[sizeX, sizeY]; // between 0 and 1

        // Start histogram coloring
        Parallel.For(0, sizeY, y => {
            for(int x = 0; x < sizeX; x++)
            {
                int i = iterationCounts[x, y];
                numIterationsPerPixel[i]++;
            }
        });

        // calculate totals for normalization
        double total = 0;
        Parallel.For(0, maxIteration, i => {
            total += numIterationsPerPixel[i];
        });

        Parallel.For(0, sizeY, y => {
            for(int x = 0; x < sizeX; x++)
            {
                int iteration = iterationCounts[x, y];
                for(int i = 0; i < iteration; i++)
                {
                    hue[x, y] += numIterationsPerPixel[i] / total;
                }
            }
        });

        // finish histogram, start coloring bitmap
        Parallel.For(0, sizeY, y => {
            for(int x = 0; x < sizeX; x++)
            {
                double huePoint = hue[x, y];
                
                //SetPixelColor(x, y, Palette(Math.Pow(huePoint, 5)));
                SetPixelColor(x, y, palette[(int)Math.Round(Math.Pow(huePoint, 5) * (maxIteration - 1))]); // exponetial coloring is more pleasing
            }
        });

    }

    public static void SetPixelColor(int x, int y, Color color)
    {
        imageBits[x + (y * sizeX)] = color.ToArgb();
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

    public static Color[] GenerateGradient(Color[] colors, int gradientSize)
    {
        Color[] gradient = new Color[gradientSize];

        for (int i = 0; i < gradientSize; i++)
        {
            double t = (double)i / (gradientSize - 1); // Interpolation parameter [0, 1]
            int colorIndex1 = (int)(t * (colors.Length - 1));
            int colorIndex2 = Math.Min(colorIndex1 + 1, colors.Length - 1);

            Color color1 = colors[colorIndex1];
            Color color2 = colors[colorIndex2];

            double t2 = (t * (colors.Length - 1)) - colorIndex1;

            int interpolatedR = (int)(color1.R + (color2.R - color1.R) * t2);
            int interpolatedG = (int)(color1.G + (color2.G - color1.G) * t2);
            int interpolatedB = (int)(color1.B + (color2.B - color1.B) * t2);

            gradient[i] = Color.FromArgb(interpolatedR, interpolatedG, interpolatedB);
        }

        return gradient;
    }
}