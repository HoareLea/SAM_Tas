// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Systems;
using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical.Tas.TPD
{
    /// <summary>
    /// The TPD-full preparation: which files the two-pass route reads and writes, which quantity crosses between
    /// the passes, and where it refuses.
    /// <para>
    /// <b>The two-pass route is deliberate and must not be removed as duplication.</b> A TPD simulation models the
    /// actual system but does not produce the <c>ResultantTemperature</c> series TM59 requires - only TBD does.
    /// So the route pays for a second TAS simulation on purpose: simulate the system, carry the first pass's
    /// result into a <b>copy</b> of the TBD, simulate that copy, and read <c>ResultantTemperature</c> from the
    /// second TSD. The expense is the point, not an oversight.
    /// </para>
    /// <para>
    /// <b>Preparation differs; assessment does not.</b> Everything this class describes ends at an
    /// <c>AnalyticalModel</c> carrying the required hourly series. From there the TPD-full route and the
    /// TSD-simple route run the identical engine-neutral <c>TM59AssessmentCalculator</c>, which knows nothing
    /// about TSD, TPD or TAS and must not learn them.
    /// </para>
    /// <para>
    /// <b>This type is deliberately free of TAS COM types.</b> It is path algebra, transfer selection and
    /// refusal - the decisions - so they can be tested without an installed TAS. The COM work stays in
    /// <see cref="Modify.CalculateResultantTemperature(string, ResultantTemperatureTransfer, out string, out string, out System.Collections.Generic.List{string})"/>.
    /// </para>
    /// </summary>
    public class ResultantTemperaturePreparation
    {
        /// <summary>
        /// The suffix distinguishing the copy the second pass owns from the design model it was copied from.
        /// <para>
        /// Named for the mechanism it uses because that is what the copy contains - a TBD whose thermostats have
        /// been overwritten with first-pass zone temperatures. It is not a design model and must never be
        /// mistaken for one.
        /// </para>
        /// </summary>
        public const string Suffix = "_TPDThermostat";

        private readonly List<string> refusals = new List<string>();

        /// <summary>
        /// Works out the two-pass route's files and whether the requested transfer can be performed.
        /// <para>
        /// <b>Pure, and it does not throw.</b> No file is opened, copied or simulated here, and no file is
        /// checked for existence - that belongs to the caller that actually does the work. What is decided here is
        /// the part that must be right before any file is touched: that the second pass writes to a path the
        /// design model does not own.
        /// </para>
        /// <para>
        /// <b>A malformed path is a refusal, not an exception.</b> The path algebra itself can throw -
        /// <c>Path.GetDirectoryName(@"C:\")</c> returns null so <c>Path.Combine</c> raises
        /// <c>ArgumentNullException</c>, and under .NET Framework an illegal character or an over-long path raises
        /// <c>ArgumentException</c> / <c>PathTooLongException</c>. The route this replaced returned false cleanly
        /// for all of those because it checked the file first, and the Grasshopper component only checks the
        /// string is non-blank, so letting them escape would surface a bare exception on a port that used to
        /// report failure.
        /// </para>
        /// </summary>
        /// <param name="path_TPD">The already-simulated TPD. Its companion TBD is expected beside it.</param>
        /// <param name="resultantTemperatureTransfer">Which quantity crosses between the passes.</param>
        public ResultantTemperaturePreparation(string path_TPD, ResultantTemperatureTransfer resultantTemperatureTransfer = ResultantTemperatureTransfer.ZoneTemperatureToThermostatLimits)
        {
            Transfer = resultantTemperatureTransfer;

            if (string.IsNullOrWhiteSpace(path_TPD))
            {
                refusals.Add("No TPD path was supplied, so the TPD-full route has nothing to prepare from.");
            }
            else
            {
                Path_TPD = path_TPD;

                try
                {
                    string directory = System.IO.Path.GetDirectoryName(path_TPD);
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(path_TPD);

                    if (directory == null || string.IsNullOrWhiteSpace(fileName))
                    {
                        refusals.Add(string.Format("'{0}' does not name a file in a directory, so the TPD-full route cannot work out where to put the TBD copy.", path_TPD));
                    }
                    else
                    {
                        //The design TBD is read only. The second pass never writes to it - see Path_TBD_Simulation.
                        Path_TBD_Design = System.IO.Path.Combine(directory, fileName + ".tbd");

                        Path_TBD_Simulation = System.IO.Path.Combine(directory, fileName + Suffix + ".tbd");
                        Path_TSD_Simulation = System.IO.Path.Combine(directory, fileName + Suffix + ".tsd");
                    }
                }
                catch (System.Exception exception)
                {
                    refusals.Add(string.Format("'{0}' is not a usable file path, so the TPD-full route cannot work out where to put the TBD copy: {1}", path_TPD, exception.Message));
                }
            }

            string refusal = TransferRefusal(resultantTemperatureTransfer);
            if (!string.IsNullOrWhiteSpace(refusal))
            {
                refusals.Add(refusal);
            }
        }

        /// <summary>The already-simulated TPD the first pass's results are read from.</summary>
        public string Path_TPD { get; }

        /// <summary>
        /// The design TBD beside the TPD. <b>Read only.</b> The route copies it and modifies the copy; the
        /// original design model is never written to, which is the invariant that keeps a TM59 preparation from
        /// quietly corrupting the model the rest of the workflow depends on.
        /// </summary>
        public string Path_TBD_Design { get; }

        /// <summary>
        /// The copy the second pass modifies and simulates. Distinct from <see cref="Path_TBD_Design"/> by
        /// construction.
        /// </summary>
        public string Path_TBD_Simulation { get; }

        /// <summary>The TSD the second pass writes, and the only place <c>ResultantTemperature</c> comes from.</summary>
        public string Path_TSD_Simulation { get; }

        /// <summary>Which quantity crosses from the first pass into the TBD copy.</summary>
        public ResultantTemperatureTransfer Transfer { get; }

        /// <summary>
        /// Whether the route can proceed. False means <b>refuse</b>: report <see cref="Refusals"/> and stop.
        /// <para>
        /// <b>There is no fall back to the approximate TPD query route.</b> That route
        /// (<see cref="ApproximateResultantTemperatureMap"/>) is a different, one-pass mechanism producing a
        /// synthesised series, and silently substituting it for a failed two-pass run would report an
        /// approximation as if it were the real thing.
        /// </para>
        /// </summary>
        public bool IsSupported => refusals.Count == 0;

        /// <summary>Why the route refused, in the caller's words. Empty when <see cref="IsSupported"/>.</summary>
        public List<string> Refusals => new List<string>(refusals);

        /// <summary>
        /// The series each zone's transfer needs, taken from the first pass's results and keyed by the zone name
        /// the TPD reports.
        /// <para>
        /// <b>This is where the route genuinely consumes the first simulation.</b> The values are the systems
        /// model's own hourly output, not a setpoint, a schedule or anything restated from the design model - so
        /// two different first-pass simulations produce two different payloads, and an empty payload means the
        /// TPD carried no usable results and the route must refuse rather than simulate a copy that would differ
        /// from the design model in no respect at all.
        /// </para>
        /// </summary>
        /// <param name="systemSpaceResults">The first pass's per-zone results, read from the simulated TPD.</param>
        public Dictionary<string, IndexedDoubles> Transferred(IEnumerable<SystemSpaceResult> systemSpaceResults)
        {
            Dictionary<string, IndexedDoubles> result = new Dictionary<string, IndexedDoubles>();

            if (systemSpaceResults == null || Transfer != ResultantTemperatureTransfer.ZoneTemperatureToThermostatLimits)
            {
                return result;
            }

            foreach (SystemSpaceResult systemSpaceResult in systemSpaceResults)
            {
                if (string.IsNullOrWhiteSpace(systemSpaceResult?.Name))
                {
                    continue;
                }

                IndexedDoubles indexedDoubles = systemSpaceResult[SpaceDataType.ZoneTemperature.ToString()];

                //A non-null but EMPTY series is skipped exactly as a missing one is. It carries nothing to
                //write into the copy's thermostats, but it would have counted towards the payload and so
                //let the route proceed - and the COM write beyond this seam reads values.Count off it,
                //which either throws out of the preparation or writes a default series while the workflow
                //reports a systems-aware answer. Absent and empty mean the same thing here: nothing to
                //transfer for that zone.
                if (indexedDoubles == null || indexedDoubles.Count <= 0)
                {
                    continue;
                }

                result[systemSpaceResult.Name] = indexedDoubles;
            }

            return result;
        }

        /// <summary>
        /// Everything the second pass needs before a TBD is opened: the route is checked, the first pass's payload
        /// is taken, and the design TBD is copied. Returns false having written <b>nothing</b>.
        /// <para>
        /// <b>Why this step exists as its own method.</b> It is the whole of the route's "may we proceed, and on
        /// which file" decision, and none of it needs a TAS COM type - so the invariants that matter most can be
        /// proved without an installed TAS: that a refusal copies nothing and leaves the design model untouched,
        /// and that where the route does proceed it proceeds on a copy. Those are exactly the guarantees a reader
        /// of the two-pass route has to be able to trust.
        /// </para>
        /// <para>
        /// <b>Order matters and is preserved.</b> The payload is taken before the copy is made, so a TPD with no
        /// usable first-pass results leaves no orphan copy behind and, more importantly, cannot lead to
        /// simulating a copy that differs from the design model in no respect at all - which would produce a
        /// plain TBD answer while reporting it as a systems-aware one.
        /// </para>
        /// </summary>
        /// <param name="systemSpaceResults">The first pass's per-zone results, read from the simulated TPD.</param>
        /// <param name="transferred">The series to write into the copy, keyed by TPD zone name. Empty on refusal.</param>
        /// <param name="refusals">Why the route stopped. Empty on success.</param>
        public bool TryBeginSecondPass(IEnumerable<SystemSpaceResult> systemSpaceResults, out Dictionary<string, IndexedDoubles> transferred, out List<string> refusals)
        {
            transferred = new Dictionary<string, IndexedDoubles>();
            refusals = Refusals;

            if (!IsSupported)
            {
                return false;
            }

            if (!System.IO.File.Exists(Path_TPD))
            {
                refusals.Add(string.Format("The TPD '{0}' does not exist.", Path_TPD));
                return false;
            }

            if (!System.IO.File.Exists(Path_TBD_Design))
            {
                refusals.Add(string.Format("The TPD-full route needs the companion TBD '{0}' beside the TPD, and it does not exist.", Path_TBD_Design));
                return false;
            }

            transferred = Transferred(systemSpaceResults);
            if (transferred.Count == 0)
            {
                refusals.Add("The TPD carries no usable first-pass results for this transfer, so there is nothing to carry into the TBD copy.");
                return false;
            }

            //The design TBD is copied here and opened for writing only through the copy's path. The original is
            //never opened for writing anywhere on this route.
            //
            //A locked, read-only or unwritable target - a previous run's copy still open in TAS is the routine
            //case - is a REFUSAL, not an exception. This method's contract is that it returns false having
            //written nothing, and Modify.CalculateResultantTemperature does not catch, so an escaping
            //IOException would surface as a bare exception on a port that reports failure everywhere else.
            try
            {
                System.IO.File.Copy(Path_TBD_Design, Path_TBD_Simulation, true);
            }
            catch (System.Exception exception)
            {
                transferred = new Dictionary<string, IndexedDoubles>();
                refusals.Add(string.Format("The design TBD '{0}' could not be copied to '{1}', so the second pass has nothing to simulate: {2}", Path_TBD_Design, Path_TBD_Simulation, exception.Message));
                return false;
            }

            return true;
        }

        /// <summary>
        /// Why a transfer cannot be performed, or null where it can.
        /// <para>
        /// <b>The limitation, established against the TAS interop rather than assumed.</b> The intended sequence
        /// is to carry the first pass's supply air temperature and supply airflow into the TBD copy. The read
        /// half is available and already happening: <c>Convert.ToSAM_SpaceSystemResults</c> asks a simulated
        /// TPD's <c>SystemZone</c> for every <c>SpaceDataType</c>, which includes
        /// <c>SupplyAirTemperature</c> and <c>FlowRate</c>. The write half has no home: <b>no type in the
        /// <c>Interop.TBD</c> object model exposes a per-zone supply air temperature.</b> A TBD zone, its
        /// internal condition, its thermostat and its internal gain were each enumerated and none has one. The
        /// temperature-valued members that TBD does have are all something else - <c>IControls</c>'s
        /// frost-protection, authority and night-setback temperatures, <c>IEmitter</c>'s outside-temperature
        /// cut-offs, <c>ISurfaceOutputSpec.dryBulbTemp</c> and <c>IWeatherYear.groundTemperature</c> - and not
        /// one of them is a zone supply condition. That is by design: TBD introduces ventilation air at outside
        /// or adjacent-zone conditions, and conditioned supply air is a TPD concept, which is precisely why TAS
        /// keeps the two models apart.
        /// </para>
        /// <para>
        /// <b>Injecting the airflow alone would be worse, not partial progress.</b> TBD's ventilation profile
        /// (<c>Profiles.ticV</c>, which SAM already reads as
        /// <c>InternalConditionParameter.SupplyAirFlow</c>) can accept an hourly series, so the airflow half is
        /// mechanically writable. But air introduced without its temperature enters at outside conditions, so
        /// transferring the flow while dropping the temperature states a system that does not exist. And once
        /// the thermostat pins the zone air temperature - which is what the supported transfer does - an
        /// injected flow changes only the plant load the second pass is not being used for, leaving
        /// <c>ResultantTemperature</c> untouched. The two halves are not independent, and half of this transfer
        /// is not a fraction of the answer.
        /// </para>
        /// <para>
        /// So the intended transfer is <b>refused</b> rather than approximated. If a future TAS release exposes a
        /// per-zone supply air temperature on the TBD side, this is the one place that has to change.
        /// </para>
        /// </summary>
        public static string TransferRefusal(ResultantTemperatureTransfer resultantTemperatureTransfer)
        {
            switch (resultantTemperatureTransfer)
            {
                case ResultantTemperatureTransfer.ZoneTemperatureToThermostatLimits:
                    return null;

                case ResultantTemperatureTransfer.SupplyAirTemperatureAndAirflow:
                    return "The TPD-full route cannot transfer supply air temperature and supply airflow into a TBD copy: the TBD object model has nowhere to put a per-zone supply air temperature, so the transfer cannot be performed accurately. Reading the first pass's supply conditions is possible; writing them is not. Use ResultantTemperatureTransfer.ZoneTemperatureToThermostatLimits, which carries the first pass's achieved zone temperature instead, and note that it is an approximation.";

                default:
                    return string.Format("The TPD-full route was asked for the transfer '{0}', which it does not implement. It never guesses which quantity to carry between the two passes.", resultantTemperatureTransfer);
            }
        }
    }
}
