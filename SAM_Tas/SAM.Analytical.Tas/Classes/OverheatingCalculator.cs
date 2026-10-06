// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.Tas
{
    /// <summary>
    /// The TM52 / TM59 overheating assessment over TAS-simulated space data.
    /// <para>
    /// <b>A compatibility wrapper.</b> The calculation itself moved to
    /// <see cref="TMOverheatingCalculator"/> in <c>SAM.Analytical</c>, because it never called TAS - it read
    /// two named hourly series off each space and produced <c>TM5x</c> results - and living here meant its
    /// tests needed a licensed TAS install for no architectural reason. This class stays so that every
    /// existing Grasshopper and user-interface caller keeps compiling and behaving identically; it adds the
    /// two things that really are TAS's:
    /// </para>
    /// <list type="number">
    /// <item>the <b>series keys TAS actually writes</b> - notably "Occupant Sensible Gain", which is not
    /// what the analytical vocabulary calls that quantity ("Occupancy Sensible Gain"). Reading the wrong one
    /// is silent: the space simply produces no assessment. Reconciling the two is deliberately left as
    /// separate work, so the wrapper pins TAS's spelling here;</item>
    /// <item>the <b>provenance</b> stamped on a result when the model is unnamed - this assembly's name, as
    /// before. Provenance only: it takes no part in any scenario, equipment or result identity.</item>
    /// </list>
    /// </summary>
    public class OverheatingCalculator
    {
        private readonly TMOverheatingCalculator tMOverheatingCalculator;

        public OverheatingCalculator(AnalyticalModel analyticalModel)
        {
            tMOverheatingCalculator = new TMOverheatingCalculator(analyticalModel)
            {
                //The keys TAS wrote, which are not what the analytical vocabulary would have called them.
                ResultantTemperatureSeriesKey = SpaceDataType.ResultantTemperature.Text(),
                OccupancySensibleGainSeriesKey = SpaceDataType.OccupantSensibleGain.Text(),

                SourceFallback = Query.Source(),
            };
        }

        public TM52BuildingCategory TM52BuildingCategory
        {
            get
            {
                return tMOverheatingCalculator.TM52BuildingCategory;
            }

            set
            {
                tMOverheatingCalculator.TM52BuildingCategory = value;
            }
        }

        public AnalyticalModel AnalyticalModel
        {
            get
            {
                return tMOverheatingCalculator.AnalyticalModel;
            }

            set
            {
                tMOverheatingCalculator.AnalyticalModel = value;
            }
        }

        public string Source
        {
            get
            {
                return tMOverheatingCalculator.Source;
            }
        }

        public TextMap TextMap
        {
            get
            {
                return tMOverheatingCalculator.TextMap;
            }

            set
            {
                tMOverheatingCalculator.TextMap = value;
            }
        }

        public List<TM52ExtendedResult> Calculate_TM52(IEnumerable<Space> spaces, int startHourOfYear = 2880, int endHourOfYear = 6528)
        {
            return tMOverheatingCalculator.Calculate_TM52(spaces, startHourOfYear, endHourOfYear);
        }

        public List<TM59ExtendedResult> Calculate_TM59(IEnumerable<Space> spaces)
        {
            return tMOverheatingCalculator.Calculate_TM59(spaces);
        }

        public IndexedDoubles GetMaxIndoorComfortTemperatures(Period period = Period.Hourly)
        {
            return tMOverheatingCalculator.GetMaxIndoorComfortTemperatures(period);
        }

        public IndexedDoubles GetMaxIndoorComfortTemperatures(int startDayIndex, int endDayIndex, Period period = Period.Hourly)
        {
            return tMOverheatingCalculator.GetMaxIndoorComfortTemperatures(startDayIndex, endDayIndex, period);
        }

        public IndexedDoubles GetMinIndoorComfortTemperatures(Period period = Period.Hourly)
        {
            return tMOverheatingCalculator.GetMinIndoorComfortTemperatures(period);
        }

        public IndexedDoubles GetMinIndoorComfortTemperatures(int startDayIndex, int endDayIndex, Period period = Period.Hourly)
        {
            return tMOverheatingCalculator.GetMinIndoorComfortTemperatures(startDayIndex, endDayIndex, period);
        }
    }
}
