namespace GGNet.Datasets
{
    public static class Iris
    {
        public enum Species
        {
            Setosa,
            Versicolor,
            Virginica
        }

        public class Point
        {
            public Species Species { get; set; }
            public double SepalLength { get; set; }
            public double SepalWidth { get; set; }
            public double PetalLength { get; set; }
            public double PetalWidth { get; set; }
        }

        public static Point[] Load() => new[]
        {
            new Point
            {
                SepalLength = 5.1,
                SepalWidth = 3.5,
                PetalLength = 1.4,
                PetalWidth = 0.2,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 4.9,
                SepalWidth = 3,
                PetalLength = 1.4,
                PetalWidth = 0.2,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 4.7,
                SepalWidth = 3.2,
                PetalLength = 1.3,
                PetalWidth = 0.2,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 4.6,
                SepalWidth = 3.1,
                PetalLength = 1.5,
                PetalWidth = 0.2,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 5,
                SepalWidth = 3.6,
                PetalLength = 1.4,
                PetalWidth = 0.2,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 5.4,
                SepalWidth = 3.9,
                PetalLength = 1.7,
                PetalWidth = 0.4,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 4.6,
                SepalWidth = 3.4,
                PetalLength = 1.4,
                PetalWidth = 0.3,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 5,
                SepalWidth = 3.4,
                PetalLength = 1.5,
                PetalWidth = 0.2,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 4.4,
                SepalWidth = 2.9,
                PetalLength = 1.4,
                PetalWidth = 0.2,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 4.9,
                SepalWidth = 3.1,
                PetalLength = 1.5,
                PetalWidth = 0.1,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 5.4,
                SepalWidth = 3.7,
                PetalLength = 1.5,
                PetalWidth = 0.2,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 4.8,
                SepalWidth = 3.4,
                PetalLength = 1.6,
                PetalWidth = 0.2,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 4.8,
                SepalWidth = 3,
                PetalLength = 1.4,
                PetalWidth = 0.1,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 4.3,
                SepalWidth = 3,
                PetalLength = 1.1,
                PetalWidth = 0.1,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 5.8,
                SepalWidth = 4,
                PetalLength = 1.2,
                PetalWidth = 0.2,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 5.7,
                SepalWidth = 4.4,
                PetalLength = 1.5,
                PetalWidth = 0.4,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 5.4,
                SepalWidth = 3.9,
                PetalLength = 1.3,
                PetalWidth = 0.4,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 5.1,
                SepalWidth = 3.5,
                PetalLength = 1.4,
                PetalWidth = 0.3,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 5.7,
                SepalWidth = 3.8,
                PetalLength = 1.7,
                PetalWidth = 0.3,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 5.1,
                SepalWidth = 3.8,
                PetalLength = 1.5,
                PetalWidth = 0.3,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 5.4,
                SepalWidth = 3.4,
                PetalLength = 1.7,
                PetalWidth = 0.2,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 5.1,
                SepalWidth = 3.7,
                PetalLength = 1.5,
                PetalWidth = 0.4,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 4.6,
                SepalWidth = 3.6,
                PetalLength = 1,
                PetalWidth = 0.2,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 5.1,
                SepalWidth = 3.3,
                PetalLength = 1.7,
                PetalWidth = 0.5,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 4.8,
                SepalWidth = 3.4,
                PetalLength = 1.9,
                PetalWidth = 0.2,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 5,
                SepalWidth = 3,
                PetalLength = 1.6,
                PetalWidth = 0.2,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 5,
                SepalWidth = 3.4,
                PetalLength = 1.6,
                PetalWidth = 0.4,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 5.2,
                SepalWidth = 3.5,
                PetalLength = 1.5,
                PetalWidth = 0.2,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 5.2,
                SepalWidth = 3.4,
                PetalLength = 1.4,
                PetalWidth = 0.2,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 4.7,
                SepalWidth = 3.2,
                PetalLength = 1.6,
                PetalWidth = 0.2,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 4.8,
                SepalWidth = 3.1,
                PetalLength = 1.6,
                PetalWidth = 0.2,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 5.4,
                SepalWidth = 3.4,
                PetalLength = 1.5,
                PetalWidth = 0.4,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 5.2,
                SepalWidth = 4.1,
                PetalLength = 1.5,
                PetalWidth = 0.1,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 5.5,
                SepalWidth = 4.2,
                PetalLength = 1.4,
                PetalWidth = 0.2,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 4.9,
                SepalWidth = 3.1,
                PetalLength = 1.5,
                PetalWidth = 0.2,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 5,
                SepalWidth = 3.2,
                PetalLength = 1.2,
                PetalWidth = 0.2,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 5.5,
                SepalWidth = 3.5,
                PetalLength = 1.3,
                PetalWidth = 0.2,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 4.9,
                SepalWidth = 3.6,
                PetalLength = 1.4,
                PetalWidth = 0.1,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 4.4,
                SepalWidth = 3,
                PetalLength = 1.3,
                PetalWidth = 0.2,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 5.1,
                SepalWidth = 3.4,
                PetalLength = 1.5,
                PetalWidth = 0.2,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 5,
                SepalWidth = 3.5,
                PetalLength = 1.3,
                PetalWidth = 0.3,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 4.5,
                SepalWidth = 2.3,
                PetalLength = 1.3,
                PetalWidth = 0.3,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 4.4,
                SepalWidth = 3.2,
                PetalLength = 1.3,
                PetalWidth = 0.2,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 5,
                SepalWidth = 3.5,
                PetalLength = 1.6,
                PetalWidth = 0.6,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 5.1,
                SepalWidth = 3.8,
                PetalLength = 1.9,
                PetalWidth = 0.4,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 4.8,
                SepalWidth = 3,
                PetalLength = 1.4,
                PetalWidth = 0.3,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 5.1,
                SepalWidth = 3.8,
                PetalLength = 1.6,
                PetalWidth = 0.2,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 4.6,
                SepalWidth = 3.2,
                PetalLength = 1.4,
                PetalWidth = 0.2,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 5.3,
                SepalWidth = 3.7,
                PetalLength = 1.5,
                PetalWidth = 0.2,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 5,
                SepalWidth = 3.3,
                PetalLength = 1.4,
                PetalWidth = 0.2,
                Species = Species.Setosa
            },
            new Point
            {
                SepalLength = 7,
                SepalWidth = 3.2,
                PetalLength = 4.7,
                PetalWidth = 1.4,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 6.4,
                SepalWidth = 3.2,
                PetalLength = 4.5,
                PetalWidth = 1.5,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 6.9,
                SepalWidth = 3.1,
                PetalLength = 4.9,
                PetalWidth = 1.5,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 5.5,
                SepalWidth = 2.3,
                PetalLength = 4,
                PetalWidth = 1.3,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 6.5,
                SepalWidth = 2.8,
                PetalLength = 4.6,
                PetalWidth = 1.5,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 5.7,
                SepalWidth = 2.8,
                PetalLength = 4.5,
                PetalWidth = 1.3,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 6.3,
                SepalWidth = 3.3,
                PetalLength = 4.7,
                PetalWidth = 1.6,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 4.9,
                SepalWidth = 2.4,
                PetalLength = 3.3,
                PetalWidth = 1,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 6.6,
                SepalWidth = 2.9,
                PetalLength = 4.6,
                PetalWidth = 1.3,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 5.2,
                SepalWidth = 2.7,
                PetalLength = 3.9,
                PetalWidth = 1.4,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 5,
                SepalWidth = 2,
                PetalLength = 3.5,
                PetalWidth = 1,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 5.9,
                SepalWidth = 3,
                PetalLength = 4.2,
                PetalWidth = 1.5,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 6,
                SepalWidth = 2.2,
                PetalLength = 4,
                PetalWidth = 1,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 6.1,
                SepalWidth = 2.9,
                PetalLength = 4.7,
                PetalWidth = 1.4,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 5.6,
                SepalWidth = 2.9,
                PetalLength = 3.6,
                PetalWidth = 1.3,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 6.7,
                SepalWidth = 3.1,
                PetalLength = 4.4,
                PetalWidth = 1.4,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 5.6,
                SepalWidth = 3,
                PetalLength = 4.5,
                PetalWidth = 1.5,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 5.8,
                SepalWidth = 2.7,
                PetalLength = 4.1,
                PetalWidth = 1,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 6.2,
                SepalWidth = 2.2,
                PetalLength = 4.5,
                PetalWidth = 1.5,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 5.6,
                SepalWidth = 2.5,
                PetalLength = 3.9,
                PetalWidth = 1.1,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 5.9,
                SepalWidth = 3.2,
                PetalLength = 4.8,
                PetalWidth = 1.8,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 6.1,
                SepalWidth = 2.8,
                PetalLength = 4,
                PetalWidth = 1.3,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 6.3,
                SepalWidth = 2.5,
                PetalLength = 4.9,
                PetalWidth = 1.5,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 6.1,
                SepalWidth = 2.8,
                PetalLength = 4.7,
                PetalWidth = 1.2,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 6.4,
                SepalWidth = 2.9,
                PetalLength = 4.3,
                PetalWidth = 1.3,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 6.6,
                SepalWidth = 3,
                PetalLength = 4.4,
                PetalWidth = 1.4,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 6.8,
                SepalWidth = 2.8,
                PetalLength = 4.8,
                PetalWidth = 1.4,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 6.7,
                SepalWidth = 3,
                PetalLength = 5,
                PetalWidth = 1.7,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 6,
                SepalWidth = 2.9,
                PetalLength = 4.5,
                PetalWidth = 1.5,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 5.7,
                SepalWidth = 2.6,
                PetalLength = 3.5,
                PetalWidth = 1,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 5.5,
                SepalWidth = 2.4,
                PetalLength = 3.8,
                PetalWidth = 1.1,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 5.5,
                SepalWidth = 2.4,
                PetalLength = 3.7,
                PetalWidth = 1,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 5.8,
                SepalWidth = 2.7,
                PetalLength = 3.9,
                PetalWidth = 1.2,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 6,
                SepalWidth = 2.7,
                PetalLength = 5.1,
                PetalWidth = 1.6,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 5.4,
                SepalWidth = 3,
                PetalLength = 4.5,
                PetalWidth = 1.5,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 6,
                SepalWidth = 3.4,
                PetalLength = 4.5,
                PetalWidth = 1.6,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 6.7,
                SepalWidth = 3.1,
                PetalLength = 4.7,
                PetalWidth = 1.5,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 6.3,
                SepalWidth = 2.3,
                PetalLength = 4.4,
                PetalWidth = 1.3,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 5.6,
                SepalWidth = 3,
                PetalLength = 4.1,
                PetalWidth = 1.3,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 5.5,
                SepalWidth = 2.5,
                PetalLength = 4,
                PetalWidth = 1.3,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 5.5,
                SepalWidth = 2.6,
                PetalLength = 4.4,
                PetalWidth = 1.2,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 6.1,
                SepalWidth = 3,
                PetalLength = 4.6,
                PetalWidth = 1.4,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 5.8,
                SepalWidth = 2.6,
                PetalLength = 4,
                PetalWidth = 1.2,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 5,
                SepalWidth = 2.3,
                PetalLength = 3.3,
                PetalWidth = 1,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 5.6,
                SepalWidth = 2.7,
                PetalLength = 4.2,
                PetalWidth = 1.3,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 5.7,
                SepalWidth = 3,
                PetalLength = 4.2,
                PetalWidth = 1.2,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 5.7,
                SepalWidth = 2.9,
                PetalLength = 4.2,
                PetalWidth = 1.3,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 6.2,
                SepalWidth = 2.9,
                PetalLength = 4.3,
                PetalWidth = 1.3,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 5.1,
                SepalWidth = 2.5,
                PetalLength = 3,
                PetalWidth = 1.1,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 5.7,
                SepalWidth = 2.8,
                PetalLength = 4.1,
                PetalWidth = 1.3,
                Species = Species.Versicolor
            },
            new Point
            {
                SepalLength = 6.3,
                SepalWidth = 3.3,
                PetalLength = 6,
                PetalWidth = 2.5,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 5.8,
                SepalWidth = 2.7,
                PetalLength = 5.1,
                PetalWidth = 1.9,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 7.1,
                SepalWidth = 3,
                PetalLength = 5.9,
                PetalWidth = 2.1,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 6.3,
                SepalWidth = 2.9,
                PetalLength = 5.6,
                PetalWidth = 1.8,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 6.5,
                SepalWidth = 3,
                PetalLength = 5.8,
                PetalWidth = 2.2,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 7.6,
                SepalWidth = 3,
                PetalLength = 6.6,
                PetalWidth = 2.1,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 4.9,
                SepalWidth = 2.5,
                PetalLength = 4.5,
                PetalWidth = 1.7,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 7.3,
                SepalWidth = 2.9,
                PetalLength = 6.3,
                PetalWidth = 1.8,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 6.7,
                SepalWidth = 2.5,
                PetalLength = 5.8,
                PetalWidth = 1.8,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 7.2,
                SepalWidth = 3.6,
                PetalLength = 6.1,
                PetalWidth = 2.5,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 6.5,
                SepalWidth = 3.2,
                PetalLength = 5.1,
                PetalWidth = 2,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 6.4,
                SepalWidth = 2.7,
                PetalLength = 5.3,
                PetalWidth = 1.9,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 6.8,
                SepalWidth = 3,
                PetalLength = 5.5,
                PetalWidth = 2.1,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 5.7,
                SepalWidth = 2.5,
                PetalLength = 5,
                PetalWidth = 2,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 5.8,
                SepalWidth = 2.8,
                PetalLength = 5.1,
                PetalWidth = 2.4,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 6.4,
                SepalWidth = 3.2,
                PetalLength = 5.3,
                PetalWidth = 2.3,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 6.5,
                SepalWidth = 3,
                PetalLength = 5.5,
                PetalWidth = 1.8,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 7.7,
                SepalWidth = 3.8,
                PetalLength = 6.7,
                PetalWidth = 2.2,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 7.7,
                SepalWidth = 2.6,
                PetalLength = 6.9,
                PetalWidth = 2.3,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 6,
                SepalWidth = 2.2,
                PetalLength = 5,
                PetalWidth = 1.5,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 6.9,
                SepalWidth = 3.2,
                PetalLength = 5.7,
                PetalWidth = 2.3,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 5.6,
                SepalWidth = 2.8,
                PetalLength = 4.9,
                PetalWidth = 2,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 7.7,
                SepalWidth = 2.8,
                PetalLength = 6.7,
                PetalWidth = 2,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 6.3,
                SepalWidth = 2.7,
                PetalLength = 4.9,
                PetalWidth = 1.8,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 6.7,
                SepalWidth = 3.3,
                PetalLength = 5.7,
                PetalWidth = 2.1,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 7.2,
                SepalWidth = 3.2,
                PetalLength = 6,
                PetalWidth = 1.8,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 6.2,
                SepalWidth = 2.8,
                PetalLength = 4.8,
                PetalWidth = 1.8,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 6.1,
                SepalWidth = 3,
                PetalLength = 4.9,
                PetalWidth = 1.8,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 6.4,
                SepalWidth = 2.8,
                PetalLength = 5.6,
                PetalWidth = 2.1,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 7.2,
                SepalWidth = 3,
                PetalLength = 5.8,
                PetalWidth = 1.6,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 7.4,
                SepalWidth = 2.8,
                PetalLength = 6.1,
                PetalWidth = 1.9,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 7.9,
                SepalWidth = 3.8,
                PetalLength = 6.4,
                PetalWidth = 2,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 6.4,
                SepalWidth = 2.8,
                PetalLength = 5.6,
                PetalWidth = 2.2,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 6.3,
                SepalWidth = 2.8,
                PetalLength = 5.1,
                PetalWidth = 1.5,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 6.1,
                SepalWidth = 2.6,
                PetalLength = 5.6,
                PetalWidth = 1.4,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 7.7,
                SepalWidth = 3,
                PetalLength = 6.1,
                PetalWidth = 2.3,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 6.3,
                SepalWidth = 3.4,
                PetalLength = 5.6,
                PetalWidth = 2.4,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 6.4,
                SepalWidth = 3.1,
                PetalLength = 5.5,
                PetalWidth = 1.8,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 6,
                SepalWidth = 3,
                PetalLength = 4.8,
                PetalWidth = 1.8,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 6.9,
                SepalWidth = 3.1,
                PetalLength = 5.4,
                PetalWidth = 2.1,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 6.7,
                SepalWidth = 3.1,
                PetalLength = 5.6,
                PetalWidth = 2.4,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 6.9,
                SepalWidth = 3.1,
                PetalLength = 5.1,
                PetalWidth = 2.3,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 5.8,
                SepalWidth = 2.7,
                PetalLength = 5.1,
                PetalWidth = 1.9,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 6.8,
                SepalWidth = 3.2,
                PetalLength = 5.9,
                PetalWidth = 2.3,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 6.7,
                SepalWidth = 3.3,
                PetalLength = 5.7,
                PetalWidth = 2.5,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 6.7,
                SepalWidth = 3,
                PetalLength = 5.2,
                PetalWidth = 2.3,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 6.3,
                SepalWidth = 2.5,
                PetalLength = 5,
                PetalWidth = 1.9,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 6.5,
                SepalWidth = 3,
                PetalLength = 5.2,
                PetalWidth = 2,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 6.2,
                SepalWidth = 3.4,
                PetalLength = 5.4,
                PetalWidth = 2.3,
                Species = Species.Virginica
            },
            new Point
            {
                SepalLength = 5.9,
                SepalWidth = 3,
                PetalLength = 5.1,
                PetalWidth = 1.8,
                Species = Species.Virginica
            }
        };
    }
}
