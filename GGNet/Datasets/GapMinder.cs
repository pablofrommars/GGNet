namespace GGNet.Datasets
{
    public static class GapMinder
    {
        public enum Continent
        {
            Africa,
            Americas,
            Asia,
            Europe,
            Oceania
        }

        public class Point
        {
            public string country { get; set; }
            public Continent continent { get; set; }
            public double lifeExp { get; set; }
            public long pop { get; set; }
            public double gdpPercap { get; set; }
        }

        public static Point[] Load() => new[]
        {
            new Point
            {
                country = "Afghanistan",
                continent = Continent.Asia,
                lifeExp = 43.828,
                pop = 31889923,
                gdpPercap = 974.5803
            },
            new Point
            {
                country = "Albania",
                continent = Continent.Europe,
                lifeExp = 76.423,
                pop = 3600523,
                gdpPercap = 5937.0295
            },
            new Point
            {
                country = "Algeria",
                continent = Continent.Africa,
                lifeExp = 72.301,
                pop = 33333216,
                gdpPercap = 6223.3675
            },
            new Point
            {
                country = "Angola",
                continent = Continent.Africa,
                lifeExp = 42.731,
                pop = 12420476,
                gdpPercap = 4797.2313
            },
            new Point
            {
                country = "Argentina",
                continent = Continent.Americas,
                lifeExp = 75.32,
                pop = 40301927,
                gdpPercap = 12779.3796
            },
            new Point
            {
                country = "Australia",
                continent = Continent.Oceania,
                lifeExp = 81.235,
                pop = 20434176,
                gdpPercap = 34435.3674
            },
            new Point
            {
                country = "Austria",
                continent = Continent.Europe,
                lifeExp = 79.829,
                pop = 8199783,
                gdpPercap = 36126.4927
            },
            new Point
            {
                country = "Bahrain",
                continent = Continent.Asia,
                lifeExp = 75.635,
                pop = 708573,
                gdpPercap = 29796.0483
            },
            new Point
            {
                country = "Bangladesh",
                continent = Continent.Asia,
                lifeExp = 64.062,
                pop = 150448339,
                gdpPercap = 1391.2538
            },
            new Point
            {
                country = "Belgium",
                continent = Continent.Europe,
                lifeExp = 79.441,
                pop = 10392226,
                gdpPercap = 33692.6051
            },
            new Point
            {
                country = "Benin",
                continent = Continent.Africa,
                lifeExp = 56.728,
                pop = 8078314,
                gdpPercap = 1441.2849
            },
            new Point
            {
                country = "Bolivia",
                continent = Continent.Americas,
                lifeExp = 65.554,
                pop = 9119152,
                gdpPercap = 3822.1371
            },
            new Point
            {
                country = "Bosnia and Herz.",
                continent = Continent.Europe,
                lifeExp = 74.852,
                pop = 4552198,
                gdpPercap = 7446.2988
            },
            new Point
            {
                country = "Botswana",
                continent = Continent.Africa,
                lifeExp = 50.728,
                pop = 1639131,
                gdpPercap = 12569.8518
            },
            new Point
            {
                country = "Brazil",
                continent = Continent.Americas,
                lifeExp = 72.39,
                pop = 190010647,
                gdpPercap = 9065.8008
            },
            new Point
            {
                country = "Bulgaria",
                continent = Continent.Europe,
                lifeExp = 73.005,
                pop = 7322858,
                gdpPercap = 10680.7928
            },
            new Point
            {
                country = "Burkina Faso",
                continent = Continent.Africa,
                lifeExp = 52.295,
                pop = 14326203,
                gdpPercap = 1217.033
            },
            new Point
            {
                country = "Burundi",
                continent = Continent.Africa,
                lifeExp = 49.58,
                pop = 8390505,
                gdpPercap = 430.0707
            },
            new Point
            {
                country = "Cambodia",
                continent = Continent.Asia,
                lifeExp = 59.723,
                pop = 14131858,
                gdpPercap = 1713.7787
            },
            new Point
            {
                country = "Cameroon",
                continent = Continent.Africa,
                lifeExp = 50.43,
                pop = 17696293,
                gdpPercap = 2042.0952
            },
            new Point
            {
                country = "Canada",
                continent = Continent.Americas,
                lifeExp = 80.653,
                pop = 33390141,
                gdpPercap = 36319.235
            },
            new Point
            {
                country = "Central African Republic",
                continent = Continent.Africa,
                lifeExp = 44.741,
                pop = 4369038,
                gdpPercap = 706.0165
            },
            new Point
            {
                country = "Chad",
                continent = Continent.Africa,
                lifeExp = 50.651,
                pop = 10238807,
                gdpPercap = 1704.0637
            },
            new Point
            {
                country = "Chile",
                continent = Continent.Americas,
                lifeExp = 78.553,
                pop = 16284741,
                gdpPercap = 13171.6388
            },
            new Point
            {
                country = "China",
                continent = Continent.Asia,
                lifeExp = 72.961,
                pop = 1318683096,
                gdpPercap = 4959.1149
            },
            new Point
            {
                country = "Colombia",
                continent = Continent.Americas,
                lifeExp = 72.889,
                pop = 44227550,
                gdpPercap = 7006.5804
            },
            new Point
            {
                country = "Comoros",
                continent = Continent.Africa,
                lifeExp = 65.152,
                pop = 710960,
                gdpPercap = 986.1479
            },
            new Point
            {
                country = "Congo, Dem. Rep.",
                continent = Continent.Africa,
                lifeExp = 46.462,
                pop = 64606759,
                gdpPercap = 277.5519
            },
            new Point
            {
                country = "Congo, Rep.",
                continent = Continent.Africa,
                lifeExp = 55.322,
                pop = 3800610,
                gdpPercap = 3632.5578
            },
            new Point
            {
                country = "Costa Rica",
                continent = Continent.Americas,
                lifeExp = 78.782,
                pop = 4133884,
                gdpPercap = 9645.0614
            },
            new Point
            {
                country = "Cote d'Ivoire",
                continent = Continent.Africa,
                lifeExp = 48.328,
                pop = 18013409,
                gdpPercap = 1544.7501
            },
            new Point
            {
                country = "Croatia",
                continent = Continent.Europe,
                lifeExp = 75.748,
                pop = 4493312,
                gdpPercap = 14619.2227
            },
            new Point
            {
                country = "Cuba",
                continent = Continent.Americas,
                lifeExp = 78.273,
                pop = 11416987,
                gdpPercap = 8948.1029
            },
            new Point
            {
                country = "Czechia",
                continent = Continent.Europe,
                lifeExp = 76.486,
                pop = 10228744,
                gdpPercap = 22833.3085
            },
            new Point
            {
                country = "Denmark",
                continent = Continent.Europe,
                lifeExp = 78.332,
                pop = 5468120,
                gdpPercap = 35278.4187
            },
            new Point
            {
                country = "Djibouti",
                continent = Continent.Africa,
                lifeExp = 54.791,
                pop = 496374,
                gdpPercap = 2082.4816
            },
            new Point
            {
                country = "Dominican Republic",
                continent = Continent.Americas,
                lifeExp = 72.235,
                pop = 9319622,
                gdpPercap = 6025.3748
            },
            new Point
            {
                country = "Ecuador",
                continent = Continent.Americas,
                lifeExp = 74.994,
                pop = 13755680,
                gdpPercap = 6873.2623
            },
            new Point
            {
                country = "Egypt",
                continent = Continent.Africa,
                lifeExp = 71.338,
                pop = 80264543,
                gdpPercap = 5581.181
            },
            new Point
            {
                country = "El Salvador",
                continent = Continent.Americas,
                lifeExp = 71.878,
                pop = 6939688,
                gdpPercap = 5728.3535
            },
            new Point
            {
                country = "Equatorial Guinea",
                continent = Continent.Africa,
                lifeExp = 51.579,
                pop = 551201,
                gdpPercap = 12154.0897
            },
            new Point
            {
                country = "Eritrea",
                continent = Continent.Africa,
                lifeExp = 58.04,
                pop = 4906585,
                gdpPercap = 641.3695
            },
            new Point
            {
                country = "Ethiopia",
                continent = Continent.Africa,
                lifeExp = 52.947,
                pop = 76511887,
                gdpPercap = 690.8056
            },
            new Point
            {
                country = "Finland",
                continent = Continent.Europe,
                lifeExp = 79.313,
                pop = 5238460,
                gdpPercap = 33207.0844
            },
            new Point
            {
                country = "France",
                continent = Continent.Europe,
                lifeExp = 80.657,
                pop = 61083916,
                gdpPercap = 30470.0167
            },
            new Point
            {
                country = "Gabon",
                continent = Continent.Africa,
                lifeExp = 56.735,
                pop = 1454867,
                gdpPercap = 13206.4845
            },
            new Point
            {
                country = "Gambia",
                continent = Continent.Africa,
                lifeExp = 59.448,
                pop = 1688359,
                gdpPercap = 752.7497
            },
            new Point
            {
                country = "Germany",
                continent = Continent.Europe,
                lifeExp = 79.406,
                pop = 82400996,
                gdpPercap = 32170.3744
            },
            new Point
            {
                country = "Ghana",
                continent = Continent.Africa,
                lifeExp = 60.022,
                pop = 22873338,
                gdpPercap = 1327.6089
            },
            new Point
            {
                country = "Greece",
                continent = Continent.Europe,
                lifeExp = 79.483,
                pop = 10706290,
                gdpPercap = 27538.4119
            },
            new Point
            {
                country = "Guatemala",
                continent = Continent.Americas,
                lifeExp = 70.259,
                pop = 12572928,
                gdpPercap = 5186.05
            },
            new Point
            {
                country = "Guinea",
                continent = Continent.Africa,
                lifeExp = 56.007,
                pop = 9947814,
                gdpPercap = 942.6542
            },
            new Point
            {
                country = "Guinea-Bissau",
                continent = Continent.Africa,
                lifeExp = 46.388,
                pop = 1472041,
                gdpPercap = 579.2317
            },
            new Point
            {
                country = "Haiti",
                continent = Continent.Americas,
                lifeExp = 60.916,
                pop = 8502814,
                gdpPercap = 1201.6372
            },
            new Point
            {
                country = "Honduras",
                continent = Continent.Americas,
                lifeExp = 70.198,
                pop = 7483763,
                gdpPercap = 3548.3308
            },
            new Point
            {
                country = "Hong Kong, China",
                continent = Continent.Asia,
                lifeExp = 82.208,
                pop = 6980412,
                gdpPercap = 39724.9787
            },
            new Point
            {
                country = "Hungary",
                continent = Continent.Europe,
                lifeExp = 73.338,
                pop = 9956108,
                gdpPercap = 18008.9444
            },
            new Point
            {
                country = "Iceland",
                continent = Continent.Europe,
                lifeExp = 81.757,
                pop = 301931,
                gdpPercap = 36180.7892
            },
            new Point
            {
                country = "India",
                continent = Continent.Asia,
                lifeExp = 64.698,
                pop = 1110396331,
                gdpPercap = 2452.2104
            },
            new Point
            {
                country = "Indonesia",
                continent = Continent.Asia,
                lifeExp = 70.65,
                pop = 223547000,
                gdpPercap = 3540.6516
            },
            new Point
            {
                country = "Iran",
                continent = Continent.Asia,
                lifeExp = 70.964,
                pop = 69453570,
                gdpPercap = 11605.7145
            },
            new Point
            {
                country = "Iraq",
                continent = Continent.Asia,
                lifeExp = 59.545,
                pop = 27499638,
                gdpPercap = 4471.0619
            },
            new Point
            {
                country = "Ireland",
                continent = Continent.Europe,
                lifeExp = 78.885,
                pop = 4109086,
                gdpPercap = 40675.9964
            },
            new Point
            {
                country = "Israel",
                continent = Continent.Asia,
                lifeExp = 80.745,
                pop = 6426679,
                gdpPercap = 25523.2771
            },
            new Point
            {
                country = "Italy",
                continent = Continent.Europe,
                lifeExp = 80.546,
                pop = 58147733,
                gdpPercap = 28569.7197
            },
            new Point
            {
                country = "Jamaica",
                continent = Continent.Americas,
                lifeExp = 72.567,
                pop = 2780132,
                gdpPercap = 7320.8803
            },
            new Point
            {
                country = "Japan",
                continent = Continent.Asia,
                lifeExp = 82.603,
                pop = 127467972,
                gdpPercap = 31656.0681
            },
            new Point
            {
                country = "Jordan",
                continent = Continent.Asia,
                lifeExp = 72.535,
                pop = 6053193,
                gdpPercap = 4519.4612
            },
            new Point
            {
                country = "Kenya",
                continent = Continent.Africa,
                lifeExp = 54.11,
                pop = 35610177,
                gdpPercap = 1463.2493
            },
            new Point
            {
                country = "Korea, Dem. Rep.",
                continent = Continent.Asia,
                lifeExp = 67.297,
                pop = 23301725,
                gdpPercap = 1593.0655
            },
            new Point
            {
                country = "Korea, Rep.",
                continent = Continent.Asia,
                lifeExp = 78.623,
                pop = 49044790,
                gdpPercap = 23348.1397
            },
            new Point
            {
                country = "Kuwait",
                continent = Continent.Asia,
                lifeExp = 77.588,
                pop = 2505559,
                gdpPercap = 47306.9898
            },
            new Point
            {
                country = "Lebanon",
                continent = Continent.Asia,
                lifeExp = 71.993,
                pop = 3921278,
                gdpPercap = 10461.0587
            },
            new Point
            {
                country = "Lesotho",
                continent = Continent.Africa,
                lifeExp = 42.592,
                pop = 2012649,
                gdpPercap = 1569.3314
            },
            new Point
            {
                country = "Liberia",
                continent = Continent.Africa,
                lifeExp = 45.678,
                pop = 3193942,
                gdpPercap = 414.5073
            },
            new Point
            {
                country = "Libya",
                continent = Continent.Africa,
                lifeExp = 73.952,
                pop = 6036914,
                gdpPercap = 12057.4993
            },
            new Point
            {
                country = "Madagascar",
                continent = Continent.Africa,
                lifeExp = 59.443,
                pop = 19167654,
                gdpPercap = 1044.7701
            },
            new Point
            {
                country = "Malawi",
                continent = Continent.Africa,
                lifeExp = 48.303,
                pop = 13327079,
                gdpPercap = 759.3499
            },
            new Point
            {
                country = "Malaysia",
                continent = Continent.Asia,
                lifeExp = 74.241,
                pop = 24821286,
                gdpPercap = 12451.6558
            },
            new Point
            {
                country = "Mali",
                continent = Continent.Africa,
                lifeExp = 54.467,
                pop = 12031795,
                gdpPercap = 1042.5816
            },
            new Point
            {
                country = "Mauritania",
                continent = Continent.Africa,
                lifeExp = 64.164,
                pop = 3270065,
                gdpPercap = 1803.1515
            },
            new Point
            {
                country = "Mauritius",
                continent = Continent.Africa,
                lifeExp = 72.801,
                pop = 1250882,
                gdpPercap = 10956.9911
            },
            new Point
            {
                country = "Mexico",
                continent = Continent.Americas,
                lifeExp = 76.195,
                pop = 108700891,
                gdpPercap = 11977.575
            },
            new Point
            {
                country = "Mongolia",
                continent = Continent.Asia,
                lifeExp = 66.803,
                pop = 2874127,
                gdpPercap = 3095.7723
            },
            new Point
            {
                country = "Montenegro",
                continent = Continent.Europe,
                lifeExp = 74.543,
                pop = 684736,
                gdpPercap = 9253.8961
            },
            new Point
            {
                country = "Morocco",
                continent = Continent.Africa,
                lifeExp = 71.164,
                pop = 33757175,
                gdpPercap = 3820.1752
            },
            new Point
            {
                country = "Mozambique",
                continent = Continent.Africa,
                lifeExp = 42.082,
                pop = 19951656,
                gdpPercap = 823.6856
            },
            new Point
            {
                country = "Myanmar",
                continent = Continent.Asia,
                lifeExp = 62.069,
                pop = 47761980,
                gdpPercap = 944
            },
            new Point
            {
                country = "Namibia",
                continent = Continent.Africa,
                lifeExp = 52.906,
                pop = 2055080,
                gdpPercap = 4811.0604
            },
            new Point
            {
                country = "Nepal",
                continent = Continent.Asia,
                lifeExp = 63.785,
                pop = 28901790,
                gdpPercap = 1091.3598
            },
            new Point
            {
                country = "Netherlands",
                continent = Continent.Europe,
                lifeExp = 79.762,
                pop = 16570613,
                gdpPercap = 36797.9333
            },
            new Point
            {
                country = "New Zealand",
                continent = Continent.Oceania,
                lifeExp = 80.204,
                pop = 4115771,
                gdpPercap = 25185.0091
            },
            new Point
            {
                country = "Nicaragua",
                continent = Continent.Americas,
                lifeExp = 72.899,
                pop = 5675356,
                gdpPercap = 2749.321
            },
            new Point
            {
                country = "Niger",
                continent = Continent.Africa,
                lifeExp = 56.867,
                pop = 12894865,
                gdpPercap = 619.6769
            },
            new Point
            {
                country = "Nigeria",
                continent = Continent.Africa,
                lifeExp = 46.859,
                pop = 135031164,
                gdpPercap = 2013.9773
            },
            new Point
            {
                country = "Norway",
                continent = Continent.Europe,
                lifeExp = 80.196,
                pop = 4627926,
                gdpPercap = 49357.1902
            },
            new Point
            {
                country = "Oman",
                continent = Continent.Asia,
                lifeExp = 75.64,
                pop = 3204897,
                gdpPercap = 22316.1929
            },
            new Point
            {
                country = "Pakistan",
                continent = Continent.Asia,
                lifeExp = 65.483,
                pop = 169270617,
                gdpPercap = 2605.9476
            },
            new Point
            {
                country = "Panama",
                continent = Continent.Americas,
                lifeExp = 75.537,
                pop = 3242173,
                gdpPercap = 9809.1856
            },
            new Point
            {
                country = "Paraguay",
                continent = Continent.Americas,
                lifeExp = 71.752,
                pop = 6667147,
                gdpPercap = 4172.8385
            },
            new Point
            {
                country = "Peru",
                continent = Continent.Americas,
                lifeExp = 71.421,
                pop = 28674757,
                gdpPercap = 7408.9056
            },
            new Point
            {
                country = "Philippines",
                continent = Continent.Asia,
                lifeExp = 71.688,
                pop = 91077287,
                gdpPercap = 3190.481
            },
            new Point
            {
                country = "Poland",
                continent = Continent.Europe,
                lifeExp = 75.563,
                pop = 38518241,
                gdpPercap = 15389.9247
            },
            new Point
            {
                country = "Portugal",
                continent = Continent.Europe,
                lifeExp = 78.098,
                pop = 10642836,
                gdpPercap = 20509.6478
            },
            new Point
            {
                country = "Puerto Rico",
                continent = Continent.Americas,
                lifeExp = 78.746,
                pop = 3942491,
                gdpPercap = 19328.709
            },
            new Point
            {
                country = "Reunion",
                continent = Continent.Africa,
                lifeExp = 76.442,
                pop = 798094,
                gdpPercap = 7670.1226
            },
            new Point
            {
                country = "Romania",
                continent = Continent.Europe,
                lifeExp = 72.476,
                pop = 22276056,
                gdpPercap = 10808.4756
            },
            new Point
            {
                country = "Rwanda",
                continent = Continent.Africa,
                lifeExp = 46.242,
                pop = 8860588,
                gdpPercap = 863.0885
            },
            new Point
            {
                country = "Sao Tome and Principe",
                continent = Continent.Africa,
                lifeExp = 65.528,
                pop = 199579,
                gdpPercap = 1598.4351
            },
            new Point
            {
                country = "Saudi Arabia",
                continent = Continent.Asia,
                lifeExp = 72.777,
                pop = 27601038,
                gdpPercap = 21654.8319
            },
            new Point
            {
                country = "Senegal",
                continent = Continent.Africa,
                lifeExp = 63.062,
                pop = 12267493,
                gdpPercap = 1712.4721
            },
            new Point
            {
                country = "Serbia",
                continent = Continent.Europe,
                lifeExp = 74.002,
                pop = 10150265,
                gdpPercap = 9786.5347
            },
            new Point
            {
                country = "Sierra Leone",
                continent = Continent.Africa,
                lifeExp = 42.568,
                pop = 6144562,
                gdpPercap = 862.5408
            },
            new Point
            {
                country = "Singapore",
                continent = Continent.Asia,
                lifeExp = 79.972,
                pop = 4553009,
                gdpPercap = 47143.1796
            },
            new Point
            {
                country = "Slovakia",
                continent = Continent.Europe,
                lifeExp = 74.663,
                pop = 5447502,
                gdpPercap = 18678.3144
            },
            new Point
            {
                country = "Slovenia",
                continent = Continent.Europe,
                lifeExp = 77.926,
                pop = 2009245,
                gdpPercap = 25768.2576
            },
            new Point
            {
                country = "Somalia",
                continent = Continent.Africa,
                lifeExp = 48.159,
                pop = 9118773,
                gdpPercap = 926.1411
            },
            new Point
            {
                country = "South Africa",
                continent = Continent.Africa,
                lifeExp = 49.339,
                pop = 43997828,
                gdpPercap = 9269.6578
            },
            new Point
            {
                country = "Spain",
                continent = Continent.Europe,
                lifeExp = 80.941,
                pop = 40448191,
                gdpPercap = 28821.0637
            },
            new Point
            {
                country = "Sri Lanka",
                continent = Continent.Asia,
                lifeExp = 72.396,
                pop = 20378239,
                gdpPercap = 3970.0954
            },
            new Point
            {
                country = "Sudan",
                continent = Continent.Africa,
                lifeExp = 58.556,
                pop = 42292929,
                gdpPercap = 2602.395
            },
            new Point
            {
                country = "Swaziland",
                continent = Continent.Africa,
                lifeExp = 39.613,
                pop = 1133066,
                gdpPercap = 4513.4806
            },
            new Point
            {
                country = "Sweden",
                continent = Continent.Europe,
                lifeExp = 80.884,
                pop = 9031088,
                gdpPercap = 33859.7484
            },
            new Point
            {
                country = "Switzerland",
                continent = Continent.Europe,
                lifeExp = 81.701,
                pop = 7554661,
                gdpPercap = 37506.4191
            },
            new Point
            {
                country = "Syria",
                continent = Continent.Asia,
                lifeExp = 74.143,
                pop = 19314747,
                gdpPercap = 4184.5481
            },
            new Point
            {
                country = "Taiwan",
                continent = Continent.Asia,
                lifeExp = 78.4,
                pop = 23174294,
                gdpPercap = 28718.2768
            },
            new Point
            {
                country = "Tanzania",
                continent = Continent.Africa,
                lifeExp = 52.517,
                pop = 38139640,
                gdpPercap = 1107.4822
            },
            new Point
            {
                country = "Thailand",
                continent = Continent.Asia,
                lifeExp = 70.616,
                pop = 65068149,
                gdpPercap = 7458.3963
            },
            new Point
            {
                country = "Togo",
                continent = Continent.Africa,
                lifeExp = 58.42,
                pop = 5701579,
                gdpPercap = 882.9699
            },
            new Point
            {
                country = "Trinidad and Tobago",
                continent = Continent.Americas,
                lifeExp = 69.819,
                pop = 1056608,
                gdpPercap = 18008.5092
            },
            new Point
            {
                country = "Tunisia",
                continent = Continent.Africa,
                lifeExp = 73.923,
                pop = 10276158,
                gdpPercap = 7092.923
            },
            new Point
            {
                country = "Turkey",
                continent = Continent.Europe,
                lifeExp = 71.777,
                pop = 71158647,
                gdpPercap = 8458.2764
            },
            new Point
            {
                country = "Uganda",
                continent = Continent.Africa,
                lifeExp = 51.542,
                pop = 29170398,
                gdpPercap = 1056.3801
            },
            new Point
            {
                country = "United Kingdom",
                continent = Continent.Europe,
                lifeExp = 79.425,
                pop = 60776238,
                gdpPercap = 33203.2613
            },
            new Point
            {
                country = "United States",
                continent = Continent.Americas,
                lifeExp = 78.242,
                pop = 301139947,
                gdpPercap = 42951.6531
            },
            new Point
            {
                country = "Uruguay",
                continent = Continent.Americas,
                lifeExp = 76.384,
                pop = 3447496,
                gdpPercap = 10611.463
            },
            new Point
            {
                country = "Venezuela",
                continent = Continent.Americas,
                lifeExp = 73.747,
                pop = 26084662,
                gdpPercap = 11415.8057
            },
            new Point
            {
                country = "Vietnam",
                continent = Continent.Asia,
                lifeExp = 74.249,
                pop = 85262356,
                gdpPercap = 2441.5764
            },
            new Point
            {
                country = "West Bank and Gaza",
                continent = Continent.Asia,
                lifeExp = 73.422,
                pop = 4018332,
                gdpPercap = 3025.3498
            },
            new Point
            {
                country = "Yemen, Rep.",
                continent = Continent.Asia,
                lifeExp = 62.698,
                pop = 22211743,
                gdpPercap = 2280.7699
            },
            new Point
            {
                country = "Zambia",
                continent = Continent.Africa,
                lifeExp = 42.384,
                pop = 11746035,
                gdpPercap = 1271.2116
            },
            new Point
            {
                country = "Zimbabwe",
                continent = Continent.Africa,
                lifeExp = 43.487,
                pop = 12311143,
                gdpPercap = 469.7093
            }
        };
    }
}
