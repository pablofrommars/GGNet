namespace GGNet.Datasets;

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

	public record Point
	{
		public string Country { get; set; } = default!;
		public Continent Continent { get; set; } = default!;
		public double LifeExp { get; set; }
		public long Pop { get; set; }
		public double GdpPercap { get; set; }
	}

	public static Point[] Load() => new[]
	{
        new Point
        {
            Country = "Afghanistan",
            Continent = Continent.Asia,
            LifeExp = 43.828,
            Pop = 31889923,
            GdpPercap = 974.5803
        },
        new Point
        {
            Country = "Albania",
            Continent = Continent.Europe,
            LifeExp = 76.423,
            Pop = 3600523,
            GdpPercap = 5937.0295
        },
        new Point
        {
            Country = "Algeria",
            Continent = Continent.Africa,
            LifeExp = 72.301,
            Pop = 33333216,
            GdpPercap = 6223.3675
        },
        new Point
        {
            Country = "Angola",
            Continent = Continent.Africa,
            LifeExp = 42.731,
            Pop = 12420476,
            GdpPercap = 4797.2313
        },
        new Point
        {
            Country = "Argentina",
            Continent = Continent.Americas,
            LifeExp = 75.32,
            Pop = 40301927,
            GdpPercap = 12779.3796
        },
        new Point
        {
            Country = "Australia",
            Continent = Continent.Oceania,
            LifeExp = 81.235,
            Pop = 20434176,
            GdpPercap = 34435.3674
        },
        new Point
        {
            Country = "Austria",
            Continent = Continent.Europe,
            LifeExp = 79.829,
            Pop = 8199783,
            GdpPercap = 36126.4927
        },
        new Point
        {
            Country = "Bahrain",
            Continent = Continent.Asia,
            LifeExp = 75.635,
            Pop = 708573,
            GdpPercap = 29796.0483
        },
        new Point
        {
            Country = "Bangladesh",
            Continent = Continent.Asia,
            LifeExp = 64.062,
            Pop = 150448339,
            GdpPercap = 1391.2538
        },
        new Point
        {
            Country = "Belgium",
            Continent = Continent.Europe,
            LifeExp = 79.441,
            Pop = 10392226,
            GdpPercap = 33692.6051
        },
        new Point
        {
            Country = "Benin",
            Continent = Continent.Africa,
            LifeExp = 56.728,
            Pop = 8078314,
            GdpPercap = 1441.2849
        },
        new Point
        {
            Country = "Bolivia",
            Continent = Continent.Americas,
            LifeExp = 65.554,
            Pop = 9119152,
            GdpPercap = 3822.1371
        },
        new Point
        {
            Country = "Bosnia and Herz.",
            Continent = Continent.Europe,
            LifeExp = 74.852,
            Pop = 4552198,
            GdpPercap = 7446.2988
        },
        new Point
        {
            Country = "Botswana",
            Continent = Continent.Africa,
            LifeExp = 50.728,
            Pop = 1639131,
            GdpPercap = 12569.8518
        },
        new Point
        {
            Country = "Brazil",
            Continent = Continent.Americas,
            LifeExp = 72.39,
            Pop = 190010647,
            GdpPercap = 9065.8008
        },
        new Point
        {
            Country = "Bulgaria",
            Continent = Continent.Europe,
            LifeExp = 73.005,
            Pop = 7322858,
            GdpPercap = 10680.7928
        },
        new Point
        {
            Country = "Burkina Faso",
            Continent = Continent.Africa,
            LifeExp = 52.295,
            Pop = 14326203,
            GdpPercap = 1217.033
        },
        new Point
        {
            Country = "Burundi",
            Continent = Continent.Africa,
            LifeExp = 49.58,
            Pop = 8390505,
            GdpPercap = 430.0707
        },
        new Point
        {
            Country = "Cambodia",
            Continent = Continent.Asia,
            LifeExp = 59.723,
            Pop = 14131858,
            GdpPercap = 1713.7787
        },
        new Point
        {
            Country = "Cameroon",
            Continent = Continent.Africa,
            LifeExp = 50.43,
            Pop = 17696293,
            GdpPercap = 2042.0952
        },
        new Point
        {
            Country = "Canada",
            Continent = Continent.Americas,
            LifeExp = 80.653,
            Pop = 33390141,
            GdpPercap = 36319.235
        },
        new Point
        {
            Country = "Central African Republic",
            Continent = Continent.Africa,
            LifeExp = 44.741,
            Pop = 4369038,
            GdpPercap = 706.0165
        },
        new Point
        {
            Country = "Chad",
            Continent = Continent.Africa,
            LifeExp = 50.651,
            Pop = 10238807,
            GdpPercap = 1704.0637
        },
        new Point
        {
            Country = "Chile",
            Continent = Continent.Americas,
            LifeExp = 78.553,
            Pop = 16284741,
            GdpPercap = 13171.6388
        },
        new Point
        {
            Country = "China",
            Continent = Continent.Asia,
            LifeExp = 72.961,
            Pop = 1318683096,
            GdpPercap = 4959.1149
        },
        new Point
        {
            Country = "Colombia",
            Continent = Continent.Americas,
            LifeExp = 72.889,
            Pop = 44227550,
            GdpPercap = 7006.5804
        },
        new Point
        {
            Country = "Comoros",
            Continent = Continent.Africa,
            LifeExp = 65.152,
            Pop = 710960,
            GdpPercap = 986.1479
        },
        new Point
        {
            Country = "Congo, Dem. Rep.",
            Continent = Continent.Africa,
            LifeExp = 46.462,
            Pop = 64606759,
            GdpPercap = 277.5519
        },
        new Point
        {
            Country = "Congo, Rep.",
            Continent = Continent.Africa,
            LifeExp = 55.322,
            Pop = 3800610,
            GdpPercap = 3632.5578
        },
        new Point
        {
            Country = "Costa Rica",
            Continent = Continent.Americas,
            LifeExp = 78.782,
            Pop = 4133884,
            GdpPercap = 9645.0614
        },
        new Point
        {
            Country = "Cote d'Ivoire",
            Continent = Continent.Africa,
            LifeExp = 48.328,
            Pop = 18013409,
            GdpPercap = 1544.7501
        },
        new Point
        {
            Country = "Croatia",
            Continent = Continent.Europe,
            LifeExp = 75.748,
            Pop = 4493312,
            GdpPercap = 14619.2227
        },
        new Point
        {
            Country = "Cuba",
            Continent = Continent.Americas,
            LifeExp = 78.273,
            Pop = 11416987,
            GdpPercap = 8948.1029
        },
        new Point
        {
            Country = "Czechia",
            Continent = Continent.Europe,
            LifeExp = 76.486,
            Pop = 10228744,
            GdpPercap = 22833.3085
        },
        new Point
        {
            Country = "Denmark",
            Continent = Continent.Europe,
            LifeExp = 78.332,
            Pop = 5468120,
            GdpPercap = 35278.4187
        },
        new Point
        {
            Country = "Djibouti",
            Continent = Continent.Africa,
            LifeExp = 54.791,
            Pop = 496374,
            GdpPercap = 2082.4816
        },
        new Point
        {
            Country = "Dominican Republic",
            Continent = Continent.Americas,
            LifeExp = 72.235,
            Pop = 9319622,
            GdpPercap = 6025.3748
        },
        new Point
        {
            Country = "Ecuador",
            Continent = Continent.Americas,
            LifeExp = 74.994,
            Pop = 13755680,
            GdpPercap = 6873.2623
        },
        new Point
        {
            Country = "Egypt",
            Continent = Continent.Africa,
            LifeExp = 71.338,
            Pop = 80264543,
            GdpPercap = 5581.181
        },
        new Point
        {
            Country = "El Salvador",
            Continent = Continent.Americas,
            LifeExp = 71.878,
            Pop = 6939688,
            GdpPercap = 5728.3535
        },
        new Point
        {
            Country = "Equatorial Guinea",
            Continent = Continent.Africa,
            LifeExp = 51.579,
            Pop = 551201,
            GdpPercap = 12154.0897
        },
        new Point
        {
            Country = "Eritrea",
            Continent = Continent.Africa,
            LifeExp = 58.04,
            Pop = 4906585,
            GdpPercap = 641.3695
        },
        new Point
        {
            Country = "Ethiopia",
            Continent = Continent.Africa,
            LifeExp = 52.947,
            Pop = 76511887,
            GdpPercap = 690.8056
        },
        new Point
        {
            Country = "Finland",
            Continent = Continent.Europe,
            LifeExp = 79.313,
            Pop = 5238460,
            GdpPercap = 33207.0844
        },
        new Point
        {
            Country = "France",
            Continent = Continent.Europe,
            LifeExp = 80.657,
            Pop = 61083916,
            GdpPercap = 30470.0167
        },
        new Point
        {
            Country = "Gabon",
            Continent = Continent.Africa,
            LifeExp = 56.735,
            Pop = 1454867,
            GdpPercap = 13206.4845
        },
        new Point
        {
            Country = "Gambia",
            Continent = Continent.Africa,
            LifeExp = 59.448,
            Pop = 1688359,
            GdpPercap = 752.7497
        },
        new Point
        {
            Country = "Germany",
            Continent = Continent.Europe,
            LifeExp = 79.406,
            Pop = 82400996,
            GdpPercap = 32170.3744
        },
        new Point
        {
            Country = "Ghana",
            Continent = Continent.Africa,
            LifeExp = 60.022,
            Pop = 22873338,
            GdpPercap = 1327.6089
        },
        new Point
        {
            Country = "Greece",
            Continent = Continent.Europe,
            LifeExp = 79.483,
            Pop = 10706290,
            GdpPercap = 27538.4119
        },
        new Point
        {
            Country = "Guatemala",
            Continent = Continent.Americas,
            LifeExp = 70.259,
            Pop = 12572928,
            GdpPercap = 5186.05
        },
        new Point
        {
            Country = "Guinea",
            Continent = Continent.Africa,
            LifeExp = 56.007,
            Pop = 9947814,
            GdpPercap = 942.6542
        },
        new Point
        {
            Country = "Guinea-Bissau",
            Continent = Continent.Africa,
            LifeExp = 46.388,
            Pop = 1472041,
            GdpPercap = 579.2317
        },
        new Point
        {
            Country = "Haiti",
            Continent = Continent.Americas,
            LifeExp = 60.916,
            Pop = 8502814,
            GdpPercap = 1201.6372
        },
        new Point
        {
            Country = "Honduras",
            Continent = Continent.Americas,
            LifeExp = 70.198,
            Pop = 7483763,
            GdpPercap = 3548.3308
        },
        new Point
        {
            Country = "Hong Kong, China",
            Continent = Continent.Asia,
            LifeExp = 82.208,
            Pop = 6980412,
            GdpPercap = 39724.9787
        },
        new Point
        {
            Country = "Hungary",
            Continent = Continent.Europe,
            LifeExp = 73.338,
            Pop = 9956108,
            GdpPercap = 18008.9444
        },
        new Point
        {
            Country = "Iceland",
            Continent = Continent.Europe,
            LifeExp = 81.757,
            Pop = 301931,
            GdpPercap = 36180.7892
        },
        new Point
        {
            Country = "India",
            Continent = Continent.Asia,
            LifeExp = 64.698,
            Pop = 1110396331,
            GdpPercap = 2452.2104
        },
        new Point
        {
            Country = "Indonesia",
            Continent = Continent.Asia,
            LifeExp = 70.65,
            Pop = 223547000,
            GdpPercap = 3540.6516
        },
        new Point
        {
            Country = "Iran",
            Continent = Continent.Asia,
            LifeExp = 70.964,
            Pop = 69453570,
            GdpPercap = 11605.7145
        },
        new Point
        {
            Country = "Iraq",
            Continent = Continent.Asia,
            LifeExp = 59.545,
            Pop = 27499638,
            GdpPercap = 4471.0619
        },
        new Point
        {
            Country = "Ireland",
            Continent = Continent.Europe,
            LifeExp = 78.885,
            Pop = 4109086,
            GdpPercap = 40675.9964
        },
        new Point
        {
            Country = "Israel",
            Continent = Continent.Asia,
            LifeExp = 80.745,
            Pop = 6426679,
            GdpPercap = 25523.2771
        },
        new Point
        {
            Country = "Italy",
            Continent = Continent.Europe,
            LifeExp = 80.546,
            Pop = 58147733,
            GdpPercap = 28569.7197
        },
        new Point
        {
            Country = "Jamaica",
            Continent = Continent.Americas,
            LifeExp = 72.567,
            Pop = 2780132,
            GdpPercap = 7320.8803
        },
        new Point
        {
            Country = "Japan",
            Continent = Continent.Asia,
            LifeExp = 82.603,
            Pop = 127467972,
            GdpPercap = 31656.0681
        },
        new Point
        {
            Country = "Jordan",
            Continent = Continent.Asia,
            LifeExp = 72.535,
            Pop = 6053193,
            GdpPercap = 4519.4612
        },
        new Point
        {
            Country = "Kenya",
            Continent = Continent.Africa,
            LifeExp = 54.11,
            Pop = 35610177,
            GdpPercap = 1463.2493
        },
        new Point
        {
            Country = "Korea, Dem. Rep.",
            Continent = Continent.Asia,
            LifeExp = 67.297,
            Pop = 23301725,
            GdpPercap = 1593.0655
        },
        new Point
        {
            Country = "Korea, Rep.",
            Continent = Continent.Asia,
            LifeExp = 78.623,
            Pop = 49044790,
            GdpPercap = 23348.1397
        },
        new Point
        {
            Country = "Kuwait",
            Continent = Continent.Asia,
            LifeExp = 77.588,
            Pop = 2505559,
            GdpPercap = 47306.9898
        },
        new Point
        {
            Country = "Lebanon",
            Continent = Continent.Asia,
            LifeExp = 71.993,
            Pop = 3921278,
            GdpPercap = 10461.0587
        },
        new Point
        {
            Country = "Lesotho",
            Continent = Continent.Africa,
            LifeExp = 42.592,
            Pop = 2012649,
            GdpPercap = 1569.3314
        },
        new Point
        {
            Country = "Liberia",
            Continent = Continent.Africa,
            LifeExp = 45.678,
            Pop = 3193942,
            GdpPercap = 414.5073
        },
        new Point
        {
            Country = "Libya",
            Continent = Continent.Africa,
            LifeExp = 73.952,
            Pop = 6036914,
            GdpPercap = 12057.4993
        },
        new Point
        {
            Country = "Madagascar",
            Continent = Continent.Africa,
            LifeExp = 59.443,
            Pop = 19167654,
            GdpPercap = 1044.7701
        },
        new Point
        {
            Country = "Malawi",
            Continent = Continent.Africa,
            LifeExp = 48.303,
            Pop = 13327079,
            GdpPercap = 759.3499
        },
        new Point
        {
            Country = "Malaysia",
            Continent = Continent.Asia,
            LifeExp = 74.241,
            Pop = 24821286,
            GdpPercap = 12451.6558
        },
        new Point
        {
            Country = "Mali",
            Continent = Continent.Africa,
            LifeExp = 54.467,
            Pop = 12031795,
            GdpPercap = 1042.5816
        },
        new Point
        {
            Country = "Mauritania",
            Continent = Continent.Africa,
            LifeExp = 64.164,
            Pop = 3270065,
            GdpPercap = 1803.1515
        },
        new Point
        {
            Country = "Mauritius",
            Continent = Continent.Africa,
            LifeExp = 72.801,
            Pop = 1250882,
            GdpPercap = 10956.9911
        },
        new Point
        {
            Country = "Mexico",
            Continent = Continent.Americas,
            LifeExp = 76.195,
            Pop = 108700891,
            GdpPercap = 11977.575
        },
        new Point
        {
            Country = "Mongolia",
            Continent = Continent.Asia,
            LifeExp = 66.803,
            Pop = 2874127,
            GdpPercap = 3095.7723
        },
        new Point
        {
            Country = "Montenegro",
            Continent = Continent.Europe,
            LifeExp = 74.543,
            Pop = 684736,
            GdpPercap = 9253.8961
        },
        new Point
        {
            Country = "Morocco",
            Continent = Continent.Africa,
            LifeExp = 71.164,
            Pop = 33757175,
            GdpPercap = 3820.1752
        },
        new Point
        {
            Country = "Mozambique",
            Continent = Continent.Africa,
            LifeExp = 42.082,
            Pop = 19951656,
            GdpPercap = 823.6856
        },
        new Point
        {
            Country = "Myanmar",
            Continent = Continent.Asia,
            LifeExp = 62.069,
            Pop = 47761980,
            GdpPercap = 944
        },
        new Point
        {
            Country = "Namibia",
            Continent = Continent.Africa,
            LifeExp = 52.906,
            Pop = 2055080,
            GdpPercap = 4811.0604
        },
        new Point
        {
            Country = "Nepal",
            Continent = Continent.Asia,
            LifeExp = 63.785,
            Pop = 28901790,
            GdpPercap = 1091.3598
        },
        new Point
        {
            Country = "Netherlands",
            Continent = Continent.Europe,
            LifeExp = 79.762,
            Pop = 16570613,
            GdpPercap = 36797.9333
        },
        new Point
        {
            Country = "New Zealand",
            Continent = Continent.Oceania,
            LifeExp = 80.204,
            Pop = 4115771,
            GdpPercap = 25185.0091
        },
        new Point
        {
            Country = "Nicaragua",
            Continent = Continent.Americas,
            LifeExp = 72.899,
            Pop = 5675356,
            GdpPercap = 2749.321
        },
        new Point
        {
            Country = "Niger",
            Continent = Continent.Africa,
            LifeExp = 56.867,
            Pop = 12894865,
            GdpPercap = 619.6769
        },
        new Point
        {
            Country = "Nigeria",
            Continent = Continent.Africa,
            LifeExp = 46.859,
            Pop = 135031164,
            GdpPercap = 2013.9773
        },
        new Point
        {
            Country = "Norway",
            Continent = Continent.Europe,
            LifeExp = 80.196,
            Pop = 4627926,
            GdpPercap = 49357.1902
        },
        new Point
        {
            Country = "Oman",
            Continent = Continent.Asia,
            LifeExp = 75.64,
            Pop = 3204897,
            GdpPercap = 22316.1929
        },
        new Point
        {
            Country = "Pakistan",
            Continent = Continent.Asia,
            LifeExp = 65.483,
            Pop = 169270617,
            GdpPercap = 2605.9476
        },
        new Point
        {
            Country = "Panama",
            Continent = Continent.Americas,
            LifeExp = 75.537,
            Pop = 3242173,
            GdpPercap = 9809.1856
        },
        new Point
        {
            Country = "Paraguay",
            Continent = Continent.Americas,
            LifeExp = 71.752,
            Pop = 6667147,
            GdpPercap = 4172.8385
        },
        new Point
        {
            Country = "Peru",
            Continent = Continent.Americas,
            LifeExp = 71.421,
            Pop = 28674757,
            GdpPercap = 7408.9056
        },
        new Point
        {
            Country = "Philippines",
            Continent = Continent.Asia,
            LifeExp = 71.688,
            Pop = 91077287,
            GdpPercap = 3190.481
        },
        new Point
        {
            Country = "Poland",
            Continent = Continent.Europe,
            LifeExp = 75.563,
            Pop = 38518241,
            GdpPercap = 15389.9247
        },
        new Point
        {
            Country = "Portugal",
            Continent = Continent.Europe,
            LifeExp = 78.098,
            Pop = 10642836,
            GdpPercap = 20509.6478
        },
        new Point
        {
            Country = "Puerto Rico",
            Continent = Continent.Americas,
            LifeExp = 78.746,
            Pop = 3942491,
            GdpPercap = 19328.709
        },
        new Point
        {
            Country = "Reunion",
            Continent = Continent.Africa,
            LifeExp = 76.442,
            Pop = 798094,
            GdpPercap = 7670.1226
        },
        new Point
        {
            Country = "Romania",
            Continent = Continent.Europe,
            LifeExp = 72.476,
            Pop = 22276056,
            GdpPercap = 10808.4756
        },
        new Point
        {
            Country = "Rwanda",
            Continent = Continent.Africa,
            LifeExp = 46.242,
            Pop = 8860588,
            GdpPercap = 863.0885
        },
        new Point
        {
            Country = "Sao Tome and Principe",
            Continent = Continent.Africa,
            LifeExp = 65.528,
            Pop = 199579,
            GdpPercap = 1598.4351
        },
        new Point
        {
            Country = "Saudi Arabia",
            Continent = Continent.Asia,
            LifeExp = 72.777,
            Pop = 27601038,
            GdpPercap = 21654.8319
        },
        new Point
        {
            Country = "Senegal",
            Continent = Continent.Africa,
            LifeExp = 63.062,
            Pop = 12267493,
            GdpPercap = 1712.4721
        },
        new Point
        {
            Country = "Serbia",
            Continent = Continent.Europe,
            LifeExp = 74.002,
            Pop = 10150265,
            GdpPercap = 9786.5347
        },
        new Point
        {
            Country = "Sierra Leone",
            Continent = Continent.Africa,
            LifeExp = 42.568,
            Pop = 6144562,
            GdpPercap = 862.5408
        },
        new Point
        {
            Country = "Singapore",
            Continent = Continent.Asia,
            LifeExp = 79.972,
            Pop = 4553009,
            GdpPercap = 47143.1796
        },
        new Point
        {
            Country = "Slovakia",
            Continent = Continent.Europe,
            LifeExp = 74.663,
            Pop = 5447502,
            GdpPercap = 18678.3144
        },
        new Point
        {
            Country = "Slovenia",
            Continent = Continent.Europe,
            LifeExp = 77.926,
            Pop = 2009245,
            GdpPercap = 25768.2576
        },
        new Point
        {
            Country = "Somalia",
            Continent = Continent.Africa,
            LifeExp = 48.159,
            Pop = 9118773,
            GdpPercap = 926.1411
        },
        new Point
        {
            Country = "South Africa",
            Continent = Continent.Africa,
            LifeExp = 49.339,
            Pop = 43997828,
            GdpPercap = 9269.6578
        },
        new Point
        {
            Country = "Spain",
            Continent = Continent.Europe,
            LifeExp = 80.941,
            Pop = 40448191,
            GdpPercap = 28821.0637
        },
        new Point
        {
            Country = "Sri Lanka",
            Continent = Continent.Asia,
            LifeExp = 72.396,
            Pop = 20378239,
            GdpPercap = 3970.0954
        },
        new Point
        {
            Country = "Sudan",
            Continent = Continent.Africa,
            LifeExp = 58.556,
            Pop = 42292929,
            GdpPercap = 2602.395
        },
        new Point
        {
            Country = "Swaziland",
            Continent = Continent.Africa,
            LifeExp = 39.613,
            Pop = 1133066,
            GdpPercap = 4513.4806
        },
        new Point
        {
            Country = "Sweden",
            Continent = Continent.Europe,
            LifeExp = 80.884,
            Pop = 9031088,
            GdpPercap = 33859.7484
        },
        new Point
        {
            Country = "Switzerland",
            Continent = Continent.Europe,
            LifeExp = 81.701,
            Pop = 7554661,
            GdpPercap = 37506.4191
        },
        new Point
        {
            Country = "Syria",
            Continent = Continent.Asia,
            LifeExp = 74.143,
            Pop = 19314747,
            GdpPercap = 4184.5481
        },
        new Point
        {
            Country = "Taiwan",
            Continent = Continent.Asia,
            LifeExp = 78.4,
            Pop = 23174294,
            GdpPercap = 28718.2768
        },
        new Point
        {
            Country = "Tanzania",
            Continent = Continent.Africa,
            LifeExp = 52.517,
            Pop = 38139640,
            GdpPercap = 1107.4822
        },
        new Point
        {
            Country = "Thailand",
            Continent = Continent.Asia,
            LifeExp = 70.616,
            Pop = 65068149,
            GdpPercap = 7458.3963
        },
        new Point
        {
            Country = "Togo",
            Continent = Continent.Africa,
            LifeExp = 58.42,
            Pop = 5701579,
            GdpPercap = 882.9699
        },
        new Point
        {
            Country = "Trinidad and Tobago",
            Continent = Continent.Americas,
            LifeExp = 69.819,
            Pop = 1056608,
            GdpPercap = 18008.5092
        },
        new Point
        {
            Country = "Tunisia",
            Continent = Continent.Africa,
            LifeExp = 73.923,
            Pop = 10276158,
            GdpPercap = 7092.923
        },
        new Point
        {
            Country = "Turkey",
            Continent = Continent.Europe,
            LifeExp = 71.777,
            Pop = 71158647,
            GdpPercap = 8458.2764
        },
        new Point
        {
            Country = "Uganda",
            Continent = Continent.Africa,
            LifeExp = 51.542,
            Pop = 29170398,
            GdpPercap = 1056.3801
        },
        new Point
        {
            Country = "United Kingdom",
            Continent = Continent.Europe,
            LifeExp = 79.425,
            Pop = 60776238,
            GdpPercap = 33203.2613
        },
        new Point
        {
            Country = "United States",
            Continent = Continent.Americas,
            LifeExp = 78.242,
            Pop = 301139947,
            GdpPercap = 42951.6531
        },
        new Point
        {
            Country = "Uruguay",
            Continent = Continent.Americas,
            LifeExp = 76.384,
            Pop = 3447496,
            GdpPercap = 10611.463
        },
        new Point
        {
            Country = "Venezuela",
            Continent = Continent.Americas,
            LifeExp = 73.747,
            Pop = 26084662,
            GdpPercap = 11415.8057
        },
        new Point
        {
            Country = "Vietnam",
            Continent = Continent.Asia,
            LifeExp = 74.249,
            Pop = 85262356,
            GdpPercap = 2441.5764
        },
        new Point
        {
            Country = "West Bank and Gaza",
            Continent = Continent.Asia,
            LifeExp = 73.422,
            Pop = 4018332,
            GdpPercap = 3025.3498
        },
        new Point
        {
            Country = "Yemen, Rep.",
            Continent = Continent.Asia,
            LifeExp = 62.698,
            Pop = 22211743,
            GdpPercap = 2280.7699
        },
        new Point
        {
            Country = "Zambia",
            Continent = Continent.Africa,
            LifeExp = 42.384,
            Pop = 11746035,
            GdpPercap = 1271.2116
        },
        new Point
        {
            Country = "Zimbabwe",
            Continent = Continent.Africa,
            LifeExp = 43.487,
            Pop = 12311143,
            GdpPercap = 469.7093
        }
    };
}