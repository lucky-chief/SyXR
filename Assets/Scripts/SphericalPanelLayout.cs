
using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SphericalPanelLayout : MonoBehaviour
{
	[Serializable]
	public class PanelSpec
	{
		public string panelName = "Panel";
		[Min(0.01f)] public float width = 1.6f;
		[Min(0.01f)] public float height = 0.9f;
		[Min(0)] public int count = 10;
	}

	[Header("Sphere")]
	[Min(0.1f)] public float radius = 5f;
	public bool wholeSphere = true;

	[Header("Region (degrees)")]
	[Range(-180f, 180f)] public float minLongitude = -180f;
	[Range(-180f, 180f)] public float maxLongitude = 180f;
	[Range(-90f, 90f)] public float minLatitude = -90f;
	[Range(-90f, 90f)] public float maxLatitude = 90f;

	[Header("Panel Types")]
	public List<PanelSpec> panelSpecs = new List<PanelSpec>
	{
		new PanelSpec { panelName = "1.6x0.9",
			width = 1.6f, height = 0.9f, count = 10 },
		new PanelSpec { panelName = "1.6x1.0",
			width = 1.6f, height = 1.0f, count = 10 },
		new PanelSpec { panelName = "0.9x1.6",
			width = 0.9f, height = 1.6f, count = 10 },
		new PanelSpec { panelName = "1.0x1.6",
			width = 1.0f, height = 1.6f, count = 10 }
	};

	[Header("Collision")]
	[Min(0f)] public float gap = 0.01f;
	[Min(0.001f)] public float panelThickness = 0.02f;

	[Header("Search")]
	[Range(1, 50)] public int searchRestarts = 8;
	[Range(10, 10000)] public int candidatesPerPanel = 400;
	[Range(1, 30)] public int refinementPasses = 3;
	[Min(0.01f)] public float initialMoveAngleDegrees = 5f;
	public int randomSeed = 12345;

	[Header("Output")]
	public bool generateObjects = true;
	public string outputRootName = "GeneratedPanels";
	public Material panelMaterial;

	[Header("Results")]
	[SerializeField] private int requestedCount;
	[SerializeField] private int placedCount;
	[SerializeField] private float placedArea;
	[SerializeField] private float estimatedCoverage;

	private Transform outputRoot;

	private class PanelData
	{
		public string name;
		public float width;
		public float height;
		public int id;
	}

	private class Placement
	{
		public PanelData panel;
		public float lon;
		public float lat;
		public Vector3 center;
		public Quaternion rotation;
		public OBB box;
	}

	private struct OBB
	{
		public Vector3 center;
		public Vector3 x, y, z;
		public Vector3 half;

		public OBB(Vector3 center, Quaternion rotation,
				   float width, float height, float thickness)
		{
			this.center = center;
			x = rotation * Vector3.right;
			y = rotation * Vector3.up;
			z = rotation * Vector3.forward;
			half = new Vector3(width, height, thickness) * 0.5f;
		}
	}

	private struct Candidate
	{
		public float lon;
		public float lat;

		public Candidate(float lon, float lat)
		{
			this.lon = lon;
			this.lat = lat;
		}
	}

	[ContextMenu("Generate Optimized Layout")]
	public void GenerateLayout()
	{
		ValidateSettings();
		ClearLayout();

		List<PanelData> jobs = BuildJobs();
		requestedCount = jobs.Count;

		if (jobs.Count == 0)
		{
			Debug.LogWarning("No panels requested.", this);
			return;
		}

		float lonMin = wholeSphere ? -180f : minLongitude;
		float lonMax = wholeSphere ? 180f : maxLongitude;
		float latMin = wholeSphere ? -90f : minLatitude;
		float latMax = wholeSphere ? 90f : maxLatitude;

		if (lonMin >= lonMax || latMin >= latMax)
		{
			Debug.LogError("Invalid longitude or latitude range.", this);
			return;
		}

		List<Placement> best = new List<Placement>();
		float bestScore = float.NegativeInfinity;

		for (int restart = 0; restart < searchRestarts; restart++)
		{
			System.Random rng =
				new System.Random(randomSeed + restart * 7919);

			List<PanelData> order = new List<PanelData>(jobs);

			// Large panels first. Randomize equal-sized panels.
			Shuffle(order, rng);
			order.Sort((a, b) =>
			{
				return (b.width * b.height).CompareTo(
					a.width * a.height);
			});

			List<Candidate> candidates = GenerateCandidates(
				rng, lonMin, lonMax, latMin, latMax,
				Mathf.Max(1000, jobs.Count * 40));

			Shuffle(candidates, rng);

			List<Placement> current = new List<Placement>();

			foreach (PanelData panel in order)
			{
				Placement bestCandidate = null;
				float candidateScore = float.NegativeInfinity;

				int tries = Mathf.Min(
					candidatesPerPanel, candidates.Count);

				int start = rng.Next(candidates.Count);

				for (int k = 0; k < tries; k++)
				{
					Candidate c = candidates[
						(start + k) % candidates.Count];

					Placement p = MakePlacement(panel, c.lon, c.lat);

					if (OverlapsAny(p.box, current))
						continue;

					float score = CompactnessScore(p, current);

					if (score > candidateScore)
					{
						candidateScore = score;
						bestCandidate = p;
					}
				}

				if (bestCandidate != null)
					current.Add(bestCandidate);
			}

			// Move panel centers over the sphere without rotating
			// their local frames away from the latitude direction.
			Refine(current, lonMin, lonMax, latMin, latMax, rng);

			float scoreFinal = EvaluateLayout(current);

			// Number of placed panels has priority over compactness.
			scoreFinal += current.Count * 1000000f;

			if (scoreFinal > bestScore)
			{
				bestScore = scoreFinal;
				best = ClonePlacements(current);
			}
		}

		placedCount = best.Count;
		placedArea = 0f;

		foreach (Placement p in best)
			placedArea += p.panel.width * p.panel.height;

		float regionArea = EstimateRegionArea(
			radius, wholeSphere ? -90f : minLatitude,
			wholeSphere ? 90f : maxLatitude);

		estimatedCoverage = regionArea > 0f
			? placedArea / regionArea
			: 0f;

		if (generateObjects)
			BuildObjects(best);

		Debug.Log(
			"Spherical layout finished. Placed " +
			placedCount + "/" + requestedCount +
			", panel area = " + placedArea.ToString("F2") +
			" m², approximate area ratio = " +
			(estimatedCoverage * 100f).ToString("F1") + "%.",
			this);

		if (placedCount < requestedCount)
		{
			Debug.LogWarning(
				"Some panels could not be placed. Increase search " +
				"budget, increase sphere radius, or reduce panel count.",
				this);
		}
	}

	private void ValidateSettings()
	{
		radius = Mathf.Max(0.1f, radius);
		gap = Mathf.Max(0f, gap);
		panelThickness = Mathf.Max(0.001f, panelThickness);
		searchRestarts = Mathf.Max(1, searchRestarts);
		candidatesPerPanel = Mathf.Max(10, candidatesPerPanel);
		refinementPasses = Mathf.Max(0, refinementPasses);
	}

	private List<PanelData> BuildJobs()
	{
		List<PanelData> jobs = new List<PanelData>();
		int id = 0;

		foreach (PanelSpec s in panelSpecs)
		{
			if (s == null || s.count <= 0 ||
				s.width <= 0f || s.height <= 0f)
				continue;

			for (int i = 0; i < s.count; i++)
			{
				jobs.Add(new PanelData
				{
					name = s.panelName + "_" + i,
					width = s.width,
					height = s.height,
					id = id++
				});
			}
		}

		return jobs;
	}

	private List<Candidate> GenerateCandidates(
		System.Random rng,
		float lonMin, float lonMax,
		float latMin, float latMax,
		int count)
	{
		List<Candidate> result = new List<Candidate>(count);

		float sinMin = Mathf.Sin(latMin * Mathf.Deg2Rad);
		float sinMax = Mathf.Sin(latMax * Mathf.Deg2Rad);

		// Uniform area sampling: uniform longitude and sin(latitude).
		for (int i = 0; i < count; i++)
		{
			float lon = Mathf.Lerp(
				lonMin, lonMax, (float)rng.NextDouble());

			float s = Mathf.Lerp(
				sinMin, sinMax, (float)rng.NextDouble());

			float lat = Mathf.Asin(
				Mathf.Clamp(s, -1f, 1f)) * Mathf.Rad2Deg;

			result.Add(new Candidate(lon, lat));
		}

		// Add staggered latitude-longitude grid points.
		// This improves coverage of gaps missed by random sampling.
		float maxSize = 1f;

		foreach (PanelSpec s in panelSpecs)
		{
			if (s == null) continue;
			maxSize = Mathf.Max(maxSize, s.width, s.height);
		}

		float latStep = Mathf.Clamp(
			maxSize / radius * Mathf.Rad2Deg * 0.55f,
			1f, 15f);

		int row = 0;

		for (float lat = latMin; lat <= latMax; lat += latStep)
		{
			float cosLat = Mathf.Max(
				0.05f, Mathf.Cos(lat * Mathf.Deg2Rad));

			float lonStep = Mathf.Clamp(
				latStep / cosLat, 1f, 30f);

			float offset = (row % 2 == 0) ? 0f : lonStep * 0.5f;

			for (float lon = lonMin + offset;
				 lon <= lonMax; lon += lonStep)
			{
				result.Add(new Candidate(lon, lat));
			}

			row++;
		}

		return result;
	}

	private Placement MakePlacement(
		PanelData panel, float lon, float lat)
	{
		Vector3 normal, east, north;
		GetFrame(lon, lat, out normal, out east, out north);

		Vector3 center = normal * radius;

		// Unity's right-handed frame:
		// local X = east (latitude tangent)
		// local Y = south
		// local Z = outward surface normal
		Quaternion rotation = Quaternion.LookRotation(
			normal, -north);

		OBB box = new OBB(
			center, rotation,
			panel.width + gap,
			panel.height + gap,
			panelThickness);

		return new Placement
		{
			panel = panel,
			lon = lon,
			lat = lat,
			center = center,
			rotation = rotation,
			box = box
		};
	}

	private static void GetFrame(
		float longitudeDeg, float latitudeDeg,
		out Vector3 normal, out Vector3 east,
		out Vector3 north)
	{
		float lon = longitudeDeg * Mathf.Deg2Rad;
		float lat = latitudeDeg * Mathf.Deg2Rad;

		float cl = Mathf.Cos(lon);
		float sl = Mathf.Sin(lon);
		float c = Mathf.Cos(lat);
		float s = Mathf.Sin(lat);

		normal = new Vector3(c * cl, s, c * sl).normalized;
		east = new Vector3(-sl, 0f, cl).normalized;
		north = new Vector3(-s * cl, c, -s * sl).normalized;
	}

	private bool OverlapsAny(OBB candidate, List<Placement> list)
	{
		foreach (Placement p in list)
		{
			if (Intersects(candidate, p.box))
				return true;
		}

		return false;
	}

	// Prefer locations with more nearby panels.
	// Among similarly populated locations, prefer shorter distances.
	private float CompactnessScore(
		Placement candidate, List<Placement> list)
	{
		if (list.Count == 0)
			return 0f;

		float target = Mathf.Max(1f, candidate.panel.width);
		float minDist = float.MaxValue;
		float nearPenalty = 0f;
		int neighbors = 0;

		foreach (Placement p in list)
		{
			float d = Vector3.Distance(
				candidate.center, p.center);

			minDist = Mathf.Min(minDist, d);

			if (d < target * 2.5f)
			{
				neighbors++;
				nearPenalty += d;
			}
		}

		return neighbors * 100f
			 - nearPenalty
			 - minDist * 0.1f;
	}

	private void Refine(
		List<Placement> list,
		float lonMin, float lonMax,
		float latMin, float latMax,
		System.Random rng)
	{
		if (list.Count < 2 || refinementPasses == 0)
			return;

		float step = initialMoveAngleDegrees;

		for (int pass = 0; pass < refinementPasses; pass++)
		{
			Shuffle(list, rng);

			foreach (Placement current in list)
			{
				Placement best = current;
				float oldScore = LocalScore(current, list);

				for (int k = 0; k < 12; k++)
				{
					float dl = ((float)rng.NextDouble() * 2f - 1f) * step;
					float dp = ((float)rng.NextDouble() * 2f - 1f) * step;

					float lon = Mathf.Clamp(
						current.lon + dl, lonMin, lonMax);

					float lat = Mathf.Clamp(
						current.lat + dp, latMin, latMax);

					Placement candidate = MakePlacement(
						current.panel, lon, lat);

					bool overlaps = false;

					foreach (Placement other in list)
					{
						if (other == current) continue;

						if (Intersects(candidate.box, other.box))
						{
							overlaps = true;
							break;
						}
					}

					if (overlaps)
						continue;

					float score = LocalScore(candidate, list);

					if (score > oldScore)
					{
						oldScore = score;
						best = candidate;
					}
				}

				current.lon = best.lon;
				current.lat = best.lat;
				current.center = best.center;
				current.rotation = best.rotation;
				current.box = best.box;
			}

			step *= 0.5f;
		}
	}

	private float LocalScore(
		Placement candidate, List<Placement> list)
	{
		float score = 0f;
		int neighbors = 0;

		foreach (Placement other in list)
		{
			if (other == candidate) continue;

			float d = Vector3.Distance(
				candidate.center, other.center);

			float target = Mathf.Max(
				candidate.panel.width,
				candidate.panel.height,
				other.panel.width,
				other.panel.height);

			if (d < target * 2.5f)
			{
				neighbors++;
				score -= d;
			}
		}

		return neighbors * 100f + score;
	}

	private float EvaluateLayout(List<Placement> list)
	{
		float score = 0f;

		for (int i = 0; i < list.Count; i++)
		{
			for (int j = i + 1; j < list.Count; j++)
			{
				float d = Vector3.Distance(
					list[i].center, list[j].center);

				score -= d;
			}
		}

		return score;
	}

	private List<Placement> ClonePlacements(List<Placement> src)
	{
		List<Placement> result = new List<Placement>(src.Count);

		foreach (Placement p in src)
		{
			result.Add(new Placement
			{
				panel = p.panel,
				lon = p.lon,
				lat = p.lat,
				center = p.center,
				rotation = p.rotation,
				box = p.box
			});
		}

		return result;
	}

	private void BuildObjects(List<Placement> list)
	{
		GameObject root = new GameObject(outputRootName);
		outputRoot = root.transform;
		outputRoot.SetParent(transform, false);

		foreach (Placement p in list)
		{
			GameObject go = GameObject.CreatePrimitive(
				PrimitiveType.Cube);

			go.name = p.panel.name;
			go.transform.SetParent(outputRoot, false);
			go.transform.localPosition = p.center;
			go.transform.localRotation = p.rotation;
			go.transform.localScale = new Vector3(
				p.panel.width, p.panel.height, panelThickness);

			if (panelMaterial != null)
				go.GetComponent<Renderer>().sharedMaterial =
					panelMaterial;
		}
	}

	[ContextMenu("Clear Layout")]
	public void ClearLayout()
	{
		Transform old = transform.Find(outputRootName);

		if (old != null)
		{
			if (Application.isPlaying)
				Destroy(old.gameObject);
			else
				DestroyImmediate(old.gameObject);
		}

		outputRoot = null;
	}

	private float EstimateRegionArea(
		float r, float latMin, float latMax)
	{
		float lonMin = wholeSphere ? -180f : minLongitude;
		float lonMax = wholeSphere ? 180f : maxLongitude;

		float deltaLon = (lonMax - lonMin) * Mathf.Deg2Rad;

		return r * r * deltaLon *
			(Mathf.Sin(latMax * Mathf.Deg2Rad) -
			 Mathf.Sin(latMin * Mathf.Deg2Rad));
	}

	private static void Shuffle<T>(
		IList<T> list, System.Random rng)
	{
		for (int i = list.Count - 1; i > 0; i--)
		{
			int j = rng.Next(i + 1);
			T temp = list[i];
			list[i] = list[j];
			list[j] = temp;
		}
	}

	// Full 15-axis separating-axis test for two 3D OBBs.
	private static bool Intersects(OBB a, OBB b)
	{
		Vector3[] A = { a.x, a.y, a.z };
		Vector3[] B = { b.x, b.y, b.z };

		float[,] R = new float[3, 3];
		float[,] AR = new float[3, 3];

		const float EPS = 1e-6f;

		for (int i = 0; i < 3; i++)
			for (int j = 0; j < 3; j++)
			{
				R[i, j] = Vector3.Dot(A[i], B[j]);
				AR[i, j] = Mathf.Abs(R[i, j]) + EPS;
			}

		Vector3 d = b.center - a.center;
		float[] t =
		{
			Vector3.Dot(d, A[0]),
			Vector3.Dot(d, A[1]),
			Vector3.Dot(d, A[2])
		};

		float[] ah = { a.half.x, a.half.y, a.half.z };
		float[] bh = { b.half.x, b.half.y, b.half.z };

		for (int i = 0; i < 3; i++)
		{
			float rb = bh[0] * AR[i, 0]
					 + bh[1] * AR[i, 1]
					 + bh[2] * AR[i, 2];

			if (Mathf.Abs(t[i]) > ah[i] + rb)
				return false;
		}

		for (int j = 0; j < 3; j++)
		{
			float projected = Mathf.Abs(
				t[0] * R[0, j] +
				t[1] * R[1, j] +
				t[2] * R[2, j]);

			float ra = ah[0] * AR[0, j]
					 + ah[1] * AR[1, j]
					 + ah[2] * AR[2, j];

			if (projected > ra + bh[j])
				return false;
		}

		for (int i = 0; i < 3; i++)
			for (int j = 0; j < 3; j++)
			{
				int i1 = (i + 1) % 3;
				int i2 = (i + 2) % 3;
				int j1 = (j + 1) % 3;
				int j2 = (j + 2) % 3;

				float ra = ah[i1] * AR[i2, j]
						 + ah[i2] * AR[i1, j];

				float rb = bh[j1] * AR[i, j2]
						 + bh[j2] * AR[i, j1];

				float projected = Mathf.Abs(
					t[i2] * R[i1, j] -
					t[i1] * R[i2, j]);

				if (projected > ra + rb)
					return false;
			}

		return true;
	}
}
