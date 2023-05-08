using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spindler.Utilities;

public static class ColorUtilities
{
    // https://github.com/wieslawsoltes/PaletteGenerator/blob/main/PaletteGenerator/Generator.cs
    public static List<Dictionary<Color, int>> KMeansCluster(Dictionary<Color, int> colors, int numClusters)
    {
        // Initialize the clusters
        var clusters = new List<Dictionary<Color, int>>();
        for (int i = 0; i < numClusters; i++)
        {
            clusters.Add(new Dictionary<Color, int>());
        }

        // Select the initial cluster centers randomly
        var centers = colors.Keys.OrderBy(c => Guid.NewGuid()).Take(numClusters).ToArray();

        // Loop until the clusters stabilize
        var changed = true;
        while (changed)
        {
            changed = false;
            // Assign each color to the nearest cluster center
            foreach (var color in colors.Keys)
            {
                var nearest = FindNearestColor(color, centers);
                var clusterIndex = Array.IndexOf(centers, nearest);
                clusters[clusterIndex][color] = colors[color];
            }

            // Recompute the cluster centers
            for (int i = 0; i < numClusters; i++)
            {
                var sumR = 0f;
                var sumG = 0f;
                var sumB = 0f;
                var count = 0;
                foreach (var color in clusters[i].Keys)
                {
                    sumR += color.Red;
                    sumG += color.Green;
                    sumB += color.Blue;
                    count++;
                }

                var r = (byte)(sumR / count);
                var g = (byte)(sumG / count);
                var b = (byte)(sumB / count);
                var newCenter = new Color(r, g, b);
                if (!newCenter.Equals(centers[i]))
                {
                    centers[i] = newCenter;
                    changed = true;
                }
            }
        }

        // Return the clusters
        return clusters;
    }

    public static Color FindNearestColor(Color color, IEnumerable<Color> centers)
    {
        Color nearest = centers.ElementAt(0);
        var minDist = double.MaxValue;

        foreach (var center in centers)
        {
            var distance = EuclidianDistance(color, center);

            if (distance < minDist)
            {
                nearest = center;
                minDist = distance;
            }
        }
        return nearest;
    }
    public static double EuclidianDistance(Color color1, Color color2)
    {
        float redDist = color1.Red - color2.Red;
        float blueDist = color1.Blue - color2.Blue;
        float greenDist = color1.Green - color2.Green;

        return Math.Sqrt(redDist * redDist + blueDist * blueDist + greenDist * greenDist);
    }

    public static double AverageLuminosity(Dictionary<Color, int> instances)
    {
        double sumLuminosity = 0;
        int totalInstances = 0;
        foreach((Color color, int instanceCount) in instances)
        {
            totalInstances += instanceCount;
            sumLuminosity += color.GetLuminosity();
        }

        return sumLuminosity / totalInstances;
    }
}
