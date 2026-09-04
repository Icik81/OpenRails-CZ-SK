// Načtení pasažérských dat

using System;
using System.Collections.Generic;
using System.IO;
using Orts.Parsers.Msts;

namespace Orts.Formats.Msts
{
    public class PassengerDataFile
    {
        public readonly List<PassengerModelDef> Models = new List<PassengerModelDef>();

        public PassengerDataFile(string filename)
        {
            if (!File.Exists(filename))
                return;

            using (STFReader stf = new STFReader(filename, false))
            {
                stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("passengers", () => { ParsePassengersBlock(stf); }),
                });
            }
        }

        void ParsePassengersBlock(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("passengeritem", () => {
                    Models.Add(new PassengerModelDef(stf));
                }),
            });
        }

        public string GetRandomShape(Random random)
        {
            if (Models == null || Models.Count == 0)
                return null;

            float totalWeight = 0;
            for (int i = 0; i < Models.Count; i++)
                totalWeight += Models[i].Weight;

            if (totalWeight <= 0)
                return Models[random.Next(Models.Count)].ShapeName;

            float roll = (float)(random.NextDouble() * totalWeight);
            float current = 0;
            for (int i = 0; i < Models.Count; i++)
            {
                current += Models[i].Weight;
                if (roll <= current)
                    return Models[i].ShapeName;
            }

            return Models[Models.Count - 1].ShapeName;
        }
    }

    public class PassengerModelDef
    {
        public string ShapeName;
        public float Weight = 1.0f;

        public PassengerModelDef(STFReader stf)
        {
            stf.MustMatch("(");
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("shape", () => { ShapeName = stf.ReadStringBlock(null); }),
                new STFReader.TokenProcessor("weight", () => { Weight = stf.ReadFloatBlock(STFReader.UNITS.None, 1.0f); }),
            });

            if (string.IsNullOrEmpty(ShapeName))
                throw new STFException(stf, "Missing Shape in PassengerItem");
        }
    }
}