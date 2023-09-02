using System;
using System.Numerics;
using System.Drawing;
using System.Drawing.Imaging;

public class MandelbrotSet
{
    public static void Main()
    {
        Bitmap bmp = new Bitmap(1000, 1000);

        int maxIterations = 200;

        List<Color> colorArray = GetGradients(Color.FromArgb(255, 255, 0, 0), Color.FromArgb(255, 0, 255, 255), maxIterations + 1);

        for(int pixX = 0; pixX < bmp.Width; pixX++)
        {
            for(int pixY = 0; pixY < bmp.Height; pixY++)
            {
                double x0 = (pixX - (0.5 * bmp.Width)) * 2;
                double y0 = (pixY - (0.5 * bmp.Height)) * 2;
                x0 /= bmp.Width;
                y0 /= bmp.Height;
                double x = 0;
                double y = 0;
                int i = 0;
                double x2 = 0;
                double y2 = 0;

                while((x2 + y2) <= 4 && i < maxIterations)
                {
                    y = 2 * x * y + y0;
                    x = x2 - y2 + x0;
                    x2 = x * x;
                    y2 = y * y;
                    i++;
                }

                //Console.WriteLine("Real: " + x.ToString() + ", Imaginary: " + y.ToString());

                bmp.SetPixel(pixX, pixY, colorArray[i]);
            }
        }

        

        bmp.Save("out.png", ImageFormat.Png);
    }

    public static List<Color> GetGradients(Color start, Color end, int steps)
    {
        List<Color> gradients = new List<Color>();
        int stepA = ((end.A - start.A) / (steps - 1));
        int stepR = ((end.R - start.R) / (steps - 1));
        int stepG = ((end.G - start.G) / (steps - 1));
        int stepB = ((end.B - start.B) / (steps - 1));

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