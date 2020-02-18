namespace GGNet.Datasets
{
    public static class Cars
    {
        public class Point
        {
            public string Model { get; set; }

            public double MPG { get; set; }

            public double Weight { get; set; }

            public int HP { get; set; }

            public int Cylinders { get; set; }
        }

        public static Point[] Load() => new[]
        {
            new Point { Model = "Mazda RX4", MPG = 21, Weight = 2.62, HP = 110, Cylinders = 6 },
            new Point { Model = "Mazda RX4 Wag", MPG = 21, Weight = 2.875, HP = 110, Cylinders = 6 },
            new Point { Model = "Datsun 710", MPG = 22.8, Weight = 2.32, HP = 93, Cylinders = 4 },
            new Point { Model = "Hornet 4 Drive", MPG = 21.4, Weight = 3.215, HP = 110, Cylinders = 6 },
            new Point { Model = "Hornet Sportabout", MPG = 18.7, Weight = 3.44, HP = 175, Cylinders = 8 },
            new Point { Model = "Valiant", MPG = 18.1, Weight = 3.46, HP = 105, Cylinders = 6 },
            new Point { Model = "Duster 360", MPG = 14.3, Weight = 3.57, HP = 245, Cylinders = 8 },
            new Point { Model = "Merc 240D", MPG = 24.4, Weight = 3.19, HP = 62, Cylinders = 4 },
            new Point { Model = "Merc 230", MPG = 22.8, Weight = 3.15, HP = 95, Cylinders = 4 },
            new Point { Model = "Merc 280", MPG = 19.2, Weight = 3.44, HP = 123, Cylinders = 6 },
            new Point { Model = "Merc 280C", MPG = 17.8, Weight = 3.44, HP = 123, Cylinders = 6 },
            new Point { Model = "Merc 450SE", MPG = 16.4, Weight = 4.07, HP = 180, Cylinders = 8 },
            new Point { Model = "Merc 450SL", MPG = 17.3, Weight = 3.73, HP = 180, Cylinders = 8 },
            new Point { Model = "Merc 450SLC", MPG = 15.2, Weight = 3.78, HP = 180, Cylinders = 8 },
            new Point { Model = "Cadillac Fleetwood", MPG = 10.4, Weight = 5.25, HP = 205, Cylinders = 8 },
            new Point { Model = "Lincoln Continental", MPG = 10.4, Weight = 5.424, HP = 215, Cylinders = 8 },
            new Point { Model = "Chrysler Imperial", MPG = 14.7, Weight = 5.345, HP = 230, Cylinders = 8 },
            new Point { Model = "Fiat 128", MPG = 32.4, Weight = 2.2, HP = 66, Cylinders = 4 },
            new Point { Model = "Honda Civic", MPG = 30.4, Weight = 1.615, HP = 52, Cylinders = 4 },
            new Point { Model = "Toyota Corolla", MPG = 33.9, Weight = 1.835, HP = 65, Cylinders = 4 },
            new Point { Model = "Toyota Corona", MPG = 21.5, Weight = 2.465, HP = 97, Cylinders = 4 },
            new Point { Model = "Dodge Challenger", MPG = 15.5, Weight = 3.52, HP = 150, Cylinders = 8 },
            new Point { Model = "AMC Javelin", MPG = 15.2, Weight = 3.435, HP = 150, Cylinders = 8 },
            new Point { Model = "Camaro Z28", MPG = 13.3, Weight = 3.84, HP = 245, Cylinders = 8 },
            new Point { Model = "Pontiac Firebird", MPG = 19.2, Weight = 3.845, HP = 175, Cylinders = 8 },
            new Point { Model = "Fiat X1-9", MPG = 27.3, Weight = 1.935, HP = 66, Cylinders = 4 },
            new Point { Model = "Porsche 914-2", MPG = 26, Weight = 2.14, HP = 91, Cylinders = 4 },
            new Point { Model = "Lotus Europa", MPG = 30.4, Weight = 1.513, HP = 113, Cylinders = 4 },
            new Point { Model = "Ford Pantera L", MPG = 15.8, Weight = 3.17, HP = 264, Cylinders = 8 },
            new Point { Model = "Ferrari Dino", MPG = 19.7, Weight = 2.77, HP = 175, Cylinders = 6 },
            new Point { Model = "Maserati Bora", MPG = 15, Weight = 3.57, HP = 335, Cylinders = 8 },
            new Point { Model = "Volvo 142E", MPG = 21.4, Weight = 2.78, HP = 109, Cylinders = 4 }
        };
    }
}
