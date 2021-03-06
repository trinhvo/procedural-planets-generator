﻿using MyEngine;
using MyEngine.Components;
using MyEngine.Events;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace MyGame.PlanetaryBody
{
	public partial class Planet
	{

		List<Vector3> verticesList = new List<Vector3>();
		public List<Vector3> GetVerticesList()
		{
			if (verticesList.Count > 0) return verticesList;

			var r = new Random();
			while (verticesList.Count < NumberOfVerticesNeededTotal)
				verticesList.Add(Vector3.Zero);
			//verticesList.Add(new Vector3d(r.NextDouble(), r.NextDouble(), r.NextDouble()).ToVector3());
			// this used to not work if we only had zeros, thus i had to use random
			return verticesList;
		}



		List<Vector4> biomesList = new List<Vector4>();
		public List<Vector4> GetDefaultBiomesList()
		{
			if (biomesList.Count > 0) return biomesList;

			while (biomesList.Count < NumberOfVerticesNeededTotal)
				biomesList.Add(Vector4.Zero);

			return biomesList;
		}



		public int NumberOfVerticesNeededTotal => (((ChunkNumberOfVerticesOnEdge - 1) * ChunkNumberOfVerticesOnEdge) / 2) + ChunkNumberOfVerticesOnEdge;

		/// <summary>
		/// top vertex index
		/// </summary>
		public int AIndex => 0;
		/// <summary>
		/// bottom left vertex index
		/// </summary>
		public int BIndex => ((ChunkNumberOfVerticesOnEdge - 1) * ChunkNumberOfVerticesOnEdge) / 2;
		/// <summary>
		/// bottom right vertex index
		/// </summary>
		public int CIndex => BIndex + (ChunkNumberOfVerticesOnEdge - 1);


		public int AIndexWithSkirts => AIndex + 4;
		public int BIndexWithSkirts => BIndex - (ChunkNumberOfVerticesOnEdge - 1) + 1;
		public int CIndexWithSkirts => BIndexWithSkirts + ((ChunkNumberOfVerticesOnEdge - 3) - 1);


		public int AIndexReal => useSkirts ? AIndexWithSkirts : AIndex;
		public int BIndexReal => useSkirts ? BIndexWithSkirts : BIndex;
		public int CIndexReal => useSkirts ? CIndexWithSkirts : CIndex;


		List<int> indiciesList;

		public List<int> GetIndiciesList()
		{
			/*
			     A
                 /\  top line
                /\/\
               /\/\/\
              /\/\/\/\ middle lines
             /\/\/\/\/\
            /\/\/\/\/\/\ bottom line
		   B           C

            */
			if (indiciesList != null) return indiciesList;

			indiciesList = new List<int>();
			// make triangles indicies list
			{
				int lineStartIndex = 0;
				int nextLineStartIndex = 1;
				indiciesList.Add(0);
				indiciesList.Add(1);
				indiciesList.Add(2);

				int numberOfVerticesInBetween = 0;
				// we skip first triangle as it was done manually
				// we skip last row of vertices as there are no triangles under it
				for (int y = 1; y < ChunkNumberOfVerticesOnEdge - 1; y++)
				{
					lineStartIndex = nextLineStartIndex;
					nextLineStartIndex = lineStartIndex + numberOfVerticesInBetween + 2;

					for (int x = 0; x <= numberOfVerticesInBetween + 1; x++)
					{
						indiciesList.Add(lineStartIndex + x);
						indiciesList.Add(nextLineStartIndex + x);
						indiciesList.Add(nextLineStartIndex + x + 1);

						if (x <= numberOfVerticesInBetween) // not a last triangle in line
						{
							indiciesList.Add(lineStartIndex + x);
							indiciesList.Add(nextLineStartIndex + x + 1);
							indiciesList.Add(lineStartIndex + x + 1);
						}
					}

					numberOfVerticesInBetween++;
				}
			}
			return indiciesList;
		}


		Mesh.VertexIndex[] skirtIndicies = null;
		public Mesh.VertexIndex[] GetEdgeVerticesIndexes()
		{
			if (skirtIndicies != null) return skirtIndicies;

			var s = new List<Mesh.VertexIndex>();
			// gather the edge vertices indicies
			{
				int lineStartIndex = 0;
				int nextLineStartIndex = 1;
				int numberOfVerticesInBetween = 0;
				s.Add(0); // first line
						  // top and all middle lines
				for (int i = 1; i < ChunkNumberOfVerticesOnEdge - 1; i++)
				{
					lineStartIndex = nextLineStartIndex;
					nextLineStartIndex = lineStartIndex + numberOfVerticesInBetween + 2;
					s.Add(lineStartIndex);
					s.Add((lineStartIndex + numberOfVerticesInBetween + 1));
					numberOfVerticesInBetween++;
				}
				// bottom line
				lineStartIndex = nextLineStartIndex;
				for (int i = 0; i < ChunkNumberOfVerticesOnEdge; i++)
				{
					s.Add((lineStartIndex + i));
				}
			}
			skirtIndicies = s.ToArray();
			return skirtIndicies;
		}
	}
}
