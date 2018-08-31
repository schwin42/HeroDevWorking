using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;

namespace VoxelMax
{
    public class ColorPalette
    {
        private static readonly string FirstLineIdentifier = "VoxelMaxColorPalette";

        public string fileName = "";
        public string paletteName="";
        public List<Color> colors=new List<Color>();

        public ColorPalette()
        {

        }
        
        public bool LoadFile(string aFileName)
        {
            this.fileName = aFileName;
            try
            {
                StreamReader sr = System.IO.File.OpenText(aFileName);
                try {
                    string firstLine = sr.ReadLine();
                    if (firstLine.Trim().ToUpper() != FirstLineIdentifier.ToUpper())
                    {
                        Debug.LogWarning("VoxelMax: One of the color palettes is in incorrect format!");
                        return false;
                    }

                    this.paletteName = sr.ReadLine();

                    string curLine = "";
                    while (!sr.EndOfStream)
                    {
                        curLine = sr.ReadLine();
                        curLine = curLine.Trim();
                        curLine = curLine.Replace(',', ' ');
                        curLine = curLine.Replace("  ", " ");
                        string[] values = curLine.Split(' ');
                        if (values.Length == 3)
                        {
                            this.colors.Add(new Color(int.Parse(values[0]) / 255f, int.Parse(values[1]) / 255f, int.Parse(values[2]) / 255f));
                        }                        
                    } 
                }
                finally
                {
                    sr.Close();
                }
                
            }
            catch
            {
                Debug.LogWarning("VoxelMax: One of the color palettes is in incorrect format!");
                return false;
            }            
            return true;
        }

        public bool SaveFile(string aFileName)
        {
            StreamWriter fs = null;
            try
            {
                try
                {
                    fs = System.IO.File.CreateText(aFileName);
                    fs.WriteLine(FirstLineIdentifier);
                    fs.WriteLine(this.paletteName);
                    foreach (Color curColor in this.colors)
                    {
                        string colorString = ((int)(curColor.r * 255)).ToString();
                        colorString += " ";
                        colorString += (int)(curColor.g * 255);
                        colorString += " ";
                        colorString += (int)(curColor.b * 255);
                        fs.WriteLine(colorString);
                    }
                }
                catch
                {
                    return false;
                }
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }            
            return true;
        }
    }

    public sealed class ColorPalettes
    {
        public List<ColorPalette> palettes = new List<ColorPalette>();
        private static readonly ColorPalettes instance = new ColorPalettes();

        private ColorPalettes()
        {
            this.LoadPalettes();
        }

        public static ColorPalettes Instance
        {
            get
            {
                return instance;
            }
        }

        public void LoadPalettes()
        {
            this.palettes.Clear();
           
            try
            {
                string[] fileNames = System.IO.Directory.GetFiles(this.GetColorPalettesPath(), "*.txt");
                foreach (string filename in fileNames)
                {
                    ColorPalette newColorPalette = new ColorPalette();
                    if (newColorPalette.LoadFile(filename))
                    {
                        palettes.Add(newColorPalette);
                    } else
                    {
                        newColorPalette = null;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("VoxelMax could not load colorpalettes. "+e.Message);
            }
        }

        public string[] GetColorPaletteNames()
        {
            string[] colorPaletteNames=new string[this.palettes.Count];           
            for (int i=0; i<this.palettes.Count; i++)
            {
                colorPaletteNames[i] = this.palettes[i].paletteName;
            }
            return colorPaletteNames;
        }

        public string GetColorPalettesPath()
        {
            string colorPalettesPath = "Assets/" + StaticValues.voxelizerRootFolder + "/" + StaticValues.colorPaletteFolder + "/";
            return colorPalettesPath;
        }

    }
}
