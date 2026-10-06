// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using NUnit.Framework;
using SAM.Core;
using SAM.Weather;
using System;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TM59.Tests
{
    /// <summary>
    /// <b>A run that states its own weather makes that weather authoritative over the design days it sizes on.</b>
    /// <para>
    /// A model's <c>CoolingDesignDays</c> / <c>HeatingDesignDays</c> parameters are DERIVED, not authored:
    /// <c>Convert.ToSAM(path_TBD, …)</c> reads the weather out of the TBD it is importing and computes them
    /// from it. So a model that came back from a Weather-A TBD carries Weather-A design days. Feeding that
    /// model back through the workflow under Weather B used to write those Weather-A design days into the
    /// Weather-B TBD, and TAS then sized on them: B1 disagreed with B2, and only B2 == B3.
    /// </para>
    /// <para>
    /// These tests pin the resolver that decides what gets written -
    /// <see cref="SAM.Analytical.Tas.Query.DesignDays_Authoritative"/> - against the four cases that matter: unchanged weather
    /// (no drift), changed weather (correct on the FIRST generation), repeated generations (a fixed point),
    /// and explicitly-stated design days (engineering intent wins).
    /// </para>
    /// <para>
    /// No TAS COM: the resolver is plain <c>SAM.Analytical</c> / <c>SAM.Weather</c> code, and the weather
    /// years below are synthesised in memory.
    /// </para>
    /// </summary>
    [TestFixture]
    public class DesignDayWeatherAuthorityTests
    {
        private const int Hours = 8760;

        private static readonly WeatherDataType[] DataTypes =
        [
            WeatherDataType.DryBulbTemperature,
            WeatherDataType.GlobalSolarRadiation,
            WeatherDataType.DiffuseSolarRadiation,
            WeatherDataType.CloudCover,
            WeatherDataType.RelativeHumidity,
            WeatherDataType.WindSpeed,
            WeatherDataType.WindDirection,
        ];

        /// <summary>
        /// A full synthetic year: one smooth annual swing plus a daily swing, so the coldest and hottest
        /// hours land on unambiguous days. <paramref name="offset"/> and <paramref name="amplitude"/> move
        /// both extremes, which is what makes two weathers produce visibly different design days.
        /// </summary>
        private static WeatherData Weather(string name, int year, double offset, double amplitude)
        {
            List<double> dryBulbs = new List<double>(Hours);
            List<double> globals = new List<double>(Hours);
            List<double> diffuses = new List<double>(Hours);
            List<double> cloudCovers = new List<double>(Hours);
            List<double> humidities = new List<double>(Hours);
            List<double> windSpeeds = new List<double>(Hours);
            List<double> windDirections = new List<double>(Hours);

            for (int i = 0; i < Hours; i++)
            {
                double annual = Math.Sin((i - 2160) * 2 * Math.PI / Hours);
                double daily = Math.Sin((i % 24 - 6) * 2 * Math.PI / 24);

                dryBulbs.Add(offset + amplitude * annual + 4 * daily);
                globals.Add(Math.Max(0, 700 * daily) * (0.6 + 0.4 * annual));
                diffuses.Add(Math.Max(0, 250 * daily));
                cloudCovers.Add(4 + 2 * daily);
                humidities.Add(70 - 10 * daily);
                windSpeeds.Add(3 + daily);
                windDirections.Add(180 + 90 * annual);
            }

            Dictionary<WeatherDataType, List<double>> values = new Dictionary<WeatherDataType, List<double>>
            {
                { WeatherDataType.DryBulbTemperature, dryBulbs },
                { WeatherDataType.GlobalSolarRadiation, globals },
                { WeatherDataType.DiffuseSolarRadiation, diffuses },
                { WeatherDataType.CloudCover, cloudCovers },
                { WeatherDataType.RelativeHumidity, humidities },
                { WeatherDataType.WindSpeed, windSpeeds },
                { WeatherDataType.WindDirection, windDirections },
            };

            WeatherYear weatherYear = SAM.Weather.Create.WeatherYear(year, values);
            Assert.That(weatherYear, Is.Not.Null, "fixture: the synthetic weather year");

            return new WeatherData(name, name + " description", 51.5, -0.45, 25, weatherYear);
        }

        /// <summary>Weather A - the seed weather, and the one the "unchanged" chain keeps using.</summary>
        private static WeatherData WeatherA()
        {
            return Weather("Weather A", 2018, 10, 12);
        }

        /// <summary>Weather B - hotter summer, milder winter, so both design days differ from A's.</summary>
        private static WeatherData WeatherB()
        {
            return Weather("Weather B", 2021, 13, 15);
        }

        /// <summary>Everything about a design day that reaches TAS: the date, plus all 24 hours of all 7 series.</summary>
        private static string Signature(DesignDay designDay)
        {
            if (designDay == null)
            {
                return "<null>";
            }

            System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
            stringBuilder.Append(designDay.Name).Append('|').Append(designDay.GetDateTime().ToString("yyyy-MM-dd")).Append('|');
            foreach (WeatherDataType weatherDataType in DataTypes)
            {
                for (int i = 0; i < 24; i++)
                {
                    stringBuilder.Append(designDay[weatherDataType, i].ToString("R", System.Globalization.CultureInfo.InvariantCulture)).Append(',');
                }
                stringBuilder.Append(';');
            }

            return stringBuilder.ToString();
        }

        private static List<string> Signatures(IEnumerable<DesignDay> designDays)
        {
            List<string> result = new List<string>();
            if (designDays != null)
            {
                foreach (DesignDay designDay in designDays)
                {
                    result.Add(Signature(designDay));
                }
            }

            return result;
        }

        /// <summary>Stands in for a model that just came back from a TBD holding <paramref name="weatherData"/>.</summary>
        private static AnalyticalModel ImportedFrom(WeatherData weatherData)
        {
            AnalyticalModel analyticalModel = new AnalyticalModel("Imported", null, null, null, new AdjacencyCluster());

            DesignDay coolingDesignDay = weatherData.CoolingDesignDay();
            DesignDay heatingDesignDay = weatherData.HeatingDesignDay();
            Assert.That(coolingDesignDay, Is.Not.Null, "fixture: the import derives a cooling design day");
            Assert.That(heatingDesignDay, Is.Not.Null, "fixture: the import derives a heating design day");

            analyticalModel.UpdateWeather(weatherData, [coolingDesignDay], [heatingDesignDay]);

            return analyticalModel;
        }

        private static AnalyticalModel Carrying(WeatherData weatherData, IEnumerable<DesignDay> coolingDesignDays, IEnumerable<DesignDay> heatingDesignDays)
        {
            AnalyticalModel analyticalModel = new AnalyticalModel("Carrying", null, null, null, new AdjacencyCluster());
            analyticalModel.UpdateWeather(weatherData, coolingDesignDays, heatingDesignDays);
            return analyticalModel;
        }

        // ------------------------------------------------------------------ the fixture itself

        [Test]
        public void Fixture_TheTwoWeathersProduceDifferentDesignDays()
        {
            WeatherData weatherData_A = WeatherA();
            WeatherData weatherData_B = WeatherB();

            Assert.That(Signature(weatherData_B.CoolingDesignDay()), Is.Not.EqualTo(Signature(weatherData_A.CoolingDesignDay())),
                "the whole test file is vacuous unless A and B imply different cooling design days");
            Assert.That(Signature(weatherData_B.HeatingDesignDay()), Is.Not.EqualTo(Signature(weatherData_A.HeatingDesignDay())),
                "the whole test file is vacuous unless A and B imply different heating design days");
        }

        [Test]
        public void Fixture_DerivingTwiceFromTheSameWeatherIsDeterministic()
        {
            Assert.That(Signature(WeatherA().CoolingDesignDay()), Is.EqualTo(Signature(WeatherA().CoolingDesignDay())));
            Assert.That(Signature(WeatherA().HeatingDesignDay()), Is.EqualTo(Signature(WeatherA().HeatingDesignDay())));
        }

        // ------------------------------------------------------------------ 1. unchanged weather

        [Test]
        public void UnchangedWeather_WritesTheSameDesignDaysTheModelAlreadyCarried()
        {
            WeatherData weatherData_A = WeatherA();
            AnalyticalModel analyticalModel = ImportedFrom(weatherData_A);

            analyticalModel.TryGetValue(Analytical.AnalyticalModelParameter.CoolingDesignDays, out SAMCollection<DesignDay> coolingDesignDays_Model);
            analyticalModel.TryGetValue(Analytical.AnalyticalModelParameter.HeatingDesignDays, out SAMCollection<DesignDay> heatingDesignDays_Model);

            SAM.Analytical.Tas.Query.DesignDays_Authoritative(analyticalModel, weatherData_A, null, null, out List<DesignDay> coolingDesignDays, out List<DesignDay> heatingDesignDays);

            Assert.That(Signatures(coolingDesignDays), Is.EqualTo(Signatures(coolingDesignDays_Model)),
                "unchanged weather must not move the cooling design day the model was imported with");
            Assert.That(Signatures(heatingDesignDays), Is.EqualTo(Signatures(heatingDesignDays_Model)),
                "unchanged weather must not move the heating design day the model was imported with");
        }

        [Test]
        public void UnchangedWeather_RepeatedGenerations_AreAFixedPoint()
        {
            WeatherData weatherData_A = WeatherA();
            AnalyticalModel analyticalModel = ImportedFrom(weatherData_A);

            List<string> signatures_Previous = null;
            for (int generation = 1; generation <= 3; generation++)
            {
                SAM.Analytical.Tas.Query.DesignDays_Authoritative(analyticalModel, weatherData_A, null, null, out List<DesignDay> coolingDesignDays, out List<DesignDay> heatingDesignDays);

                List<string> signatures = Signatures(coolingDesignDays);
                signatures.AddRange(Signatures(heatingDesignDays));

                if (signatures_Previous != null)
                {
                    Assert.That(signatures, Is.EqualTo(signatures_Previous), "generation " + generation + " under unchanged weather");
                }

                signatures_Previous = signatures;

                // the next generation re-imports the TBD this one wrote, so it carries these design days
                analyticalModel = Carrying(weatherData_A, coolingDesignDays, heatingDesignDays);
            }
        }

        // ------------------------------------------------------------------ 2. changed weather, first generation

        [Test]
        public void ChangedWeather_TheFirstGenerationAlreadyUsesTheNewWeathersDesignDays()
        {
            WeatherData weatherData_A = WeatherA();
            WeatherData weatherData_B = WeatherB();

            AnalyticalModel analyticalModel = ImportedFrom(weatherData_A);

            SAM.Analytical.Tas.Query.DesignDays_Authoritative(analyticalModel, weatherData_B, null, null, out List<DesignDay> coolingDesignDays, out List<DesignDay> heatingDesignDays);

            Assert.That(Signatures(coolingDesignDays), Is.EqualTo(new List<string> { Signature(weatherData_B.CoolingDesignDay()) }),
                "the FIRST Weather-B generation must size on Weather B's cooling design day");
            Assert.That(Signatures(heatingDesignDays), Is.EqualTo(new List<string> { Signature(weatherData_B.HeatingDesignDay()) }),
                "the FIRST Weather-B generation must size on Weather B's heating design day");

            Assert.That(Signatures(coolingDesignDays), Is.Not.EqualTo(new List<string> { Signature(weatherData_A.CoolingDesignDay()) }),
                "the defect: Weather A's cooling design day survived into the Weather-B run");
            Assert.That(Signatures(heatingDesignDays), Is.Not.EqualTo(new List<string> { Signature(weatherData_A.HeatingDesignDay()) }),
                "the defect: Weather A's heating design day survived into the Weather-B run");
        }

        // ------------------------------------------------------------------ 3. B1 == B2 == B3

        [Test]
        public void ChangedWeather_B1_B2_B3_AreATrueFixedPoint()
        {
            WeatherData weatherData_B = WeatherB();

            // B1 starts from a model imported out of the Weather-A TBD.
            AnalyticalModel analyticalModel = ImportedFrom(WeatherA());

            List<string> signatures_B1 = null;
            List<string> signatures_Previous = null;

            for (int generation = 1; generation <= 3; generation++)
            {
                SAM.Analytical.Tas.Query.DesignDays_Authoritative(analyticalModel, weatherData_B, null, null, out List<DesignDay> coolingDesignDays, out List<DesignDay> heatingDesignDays);

                List<string> signatures = Signatures(coolingDesignDays);
                signatures.AddRange(Signatures(heatingDesignDays));

                if (generation == 1)
                {
                    signatures_B1 = signatures;
                }
                else
                {
                    Assert.That(signatures, Is.EqualTo(signatures_Previous), "B" + generation + " must equal B" + (generation - 1));
                    Assert.That(signatures, Is.EqualTo(signatures_B1), "B" + generation + " must equal B1 - B1 is not allowed to be the odd one out");
                }

                signatures_Previous = signatures;

                // generation N+1 imports the TBD generation N wrote: Weather B, and these design days.
                analyticalModel = Carrying(weatherData_B, coolingDesignDays, heatingDesignDays);
            }
        }

        // ------------------------------------------------------------------ 4. explicit design days are intent

        [Test]
        public void ExplicitDesignDays_WinOverTheRunsWeather()
        {
            WeatherData weatherData_A = WeatherA();
            WeatherData weatherData_B = WeatherB();

            AnalyticalModel analyticalModel = ImportedFrom(weatherData_A);

            // What SAMAnalytical.DesignDays produces when the engineer overrides the heating temperature.
            DesignDay heatingDesignDay_Custom = weatherData_B.HeatingDesignDay();
            for (int i = 0; i < 24; i++)
            {
                heatingDesignDay_Custom[WeatherDataType.DryBulbTemperature, i] = -8;
            }

            SAM.Analytical.Tas.Query.DesignDays_Authoritative(analyticalModel, weatherData_B, null, [heatingDesignDay_Custom], out List<DesignDay> coolingDesignDays, out List<DesignDay> heatingDesignDays);

            Assert.That(Signatures(heatingDesignDays), Is.EqualTo(new List<string> { Signature(heatingDesignDay_Custom) }),
                "a design day the caller stated outright is engineering intent and must survive the run's weather");
            Assert.That(Signatures(coolingDesignDays), Is.EqualTo(new List<string> { Signature(weatherData_B.CoolingDesignDay()) }),
                "the slot the caller did NOT state still comes from the run's weather");
        }

        // ------------------------------------------------------------------ 5. no weather of its own

        [Test]
        public void NoWeatherOfItsOwn_LeavesTheModelsDesignDaysAlone()
        {
            WeatherData weatherData_A = WeatherA();

            DesignDay heatingDesignDay_Custom = weatherData_A.HeatingDesignDay();
            for (int i = 0; i < 24; i++)
            {
                heatingDesignDay_Custom[WeatherDataType.DryBulbTemperature, i] = -8;
            }

            AnalyticalModel analyticalModel = Carrying(weatherData_A, [weatherData_A.CoolingDesignDay()], [heatingDesignDay_Custom]);

            SAM.Analytical.Tas.Query.DesignDays_Authoritative(analyticalModel, null, null, null, out List<DesignDay> coolingDesignDays, out List<DesignDay> heatingDesignDays);

            Assert.That(Signatures(heatingDesignDays), Is.EqualTo(new List<string> { Signature(heatingDesignDay_Custom) }),
                "a run that states no weather has nothing to rebind against - the model's design days stand");
            Assert.That(Signatures(coolingDesignDays), Is.EqualTo(new List<string> { Signature(weatherData_A.CoolingDesignDay()) }));
        }

        // ------------------------------------------------------------------ degenerate inputs

        [Test]
        public void AuthoritativeWeatherThatYieldsNoDesignDay_FallsBackToTheModel()
        {
            WeatherData weatherData_A = WeatherA();
            AnalyticalModel analyticalModel = ImportedFrom(weatherData_A);

            // An empty (but present) weather-year list, so neither design day can be derived from it.
            // Note the 6-argument constructor: the 5-argument one leaves the list null, and
            // SAM.Weather.WeatherData.WeatherYears then throws rather than returning nothing.
            WeatherData weatherData_Empty = new WeatherData("Empty", null, 0, 0, 0, null);
            Assert.That(weatherData_Empty.CoolingDesignDay(), Is.Null, "fixture: an empty weather yields no design day");

            SAM.Analytical.Tas.Query.DesignDays_Authoritative(analyticalModel, weatherData_Empty, null, null, out List<DesignDay> coolingDesignDays, out List<DesignDay> heatingDesignDays);

            Assert.That(Signatures(coolingDesignDays), Is.EqualTo(new List<string> { Signature(weatherData_A.CoolingDesignDay()) }),
                "sizing on the previous weather's design day beats sizing on none - TAS answers no design days with a zero load");
            Assert.That(Signatures(heatingDesignDays), Is.EqualTo(new List<string> { Signature(weatherData_A.HeatingDesignDay()) }));
        }

        [Test]
        public void NoModelAndNoWeather_YieldsNothing()
        {
            SAM.Analytical.Tas.Query.DesignDays_Authoritative(null, null, null, null, out List<DesignDay> coolingDesignDays, out List<DesignDay> heatingDesignDays);

            Assert.That(coolingDesignDays, Is.Null);
            Assert.That(heatingDesignDays, Is.Null);
        }

        [Test]
        public void AModelCarryingNoDesignDays_TakesThemFromTheRunsWeather()
        {
            WeatherData weatherData_A = WeatherA();

            // The seed case: a model authored outside TAS. Before the fix this produced a TBD with NO design
            // days at all, and TAS sized every zone to a zero load.
            AnalyticalModel analyticalModel = new AnalyticalModel("Seed", null, null, null, new AdjacencyCluster());

            SAM.Analytical.Tas.Query.DesignDays_Authoritative(analyticalModel, weatherData_A, null, null, out List<DesignDay> coolingDesignDays, out List<DesignDay> heatingDesignDays);

            Assert.That(Signatures(coolingDesignDays), Is.EqualTo(new List<string> { Signature(weatherData_A.CoolingDesignDay()) }));
            Assert.That(Signatures(heatingDesignDays), Is.EqualTo(new List<string> { Signature(weatherData_A.HeatingDesignDay()) }));
        }

        [Test]
        public void EmptyExplicitCollections_ReadAsNotStated()
        {
            WeatherData weatherData_A = WeatherA();

            SAM.Analytical.Tas.Query.DesignDays_Authoritative(null, weatherData_A, new List<DesignDay>(), new List<DesignDay>(), out List<DesignDay> coolingDesignDays, out List<DesignDay> heatingDesignDays);

            Assert.That(Signatures(coolingDesignDays), Is.EqualTo(new List<string> { Signature(weatherData_A.CoolingDesignDay()) }),
                "an empty list is the WorkflowgbXML component's way of saying the input was left unwired");
            Assert.That(Signatures(heatingDesignDays), Is.EqualTo(new List<string> { Signature(weatherData_A.HeatingDesignDay()) }));
        }

        // ------------------------------------------------------------------ 6. the companion-selection rule stays put

        /// <summary>
        /// The design-day companions are only safe to leave out of the space's condition because
        /// <see cref="SAM.Analytical.Tas.Query.PrimaryInternalConditionIndex"/> picks the normal one. Re-pinned here because
        /// this work is the first to touch the design-day seam since that rule was stated.
        /// </summary>
        [Test]
        public void PrimaryInternalConditionIndex_StillPrefersTheNormalConditionOverItsDesignDayCompanions()
        {
            List<InternalCondition> internalConditions =
            [
                new InternalCondition("Studio 1_0 - HDD"),
                new InternalCondition("Studio 1_0 - CDD"),
                new InternalCondition("Studio 1_0"),
            ];

            Assert.That(SAM.Analytical.Tas.Query.PrimaryInternalConditionIndex(internalConditions), Is.EqualTo(2));

            Assert.That(SAM.Analytical.Tas.Query.PrimaryInternalConditionIndex([new InternalCondition("Studio 1_0 - HDD")]), Is.EqualTo(0),
                "all companions and nothing else: fall back to the first rather than to nothing");
            Assert.That(SAM.Analytical.Tas.Query.PrimaryInternalConditionIndex(new List<InternalCondition>()), Is.EqualTo(-1));
            Assert.That(SAM.Analytical.Tas.Query.PrimaryInternalConditionIndex(null), Is.EqualTo(-1));
        }
    }
}
