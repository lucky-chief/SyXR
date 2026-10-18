using System.Collections.Generic;
using UnityEngine;

public class SphereLayout : MonoBehaviour
{
	[Header("球面参数")]
	public float radius = 10f;

	[Header("各尺寸数量")]
	public int count160x090 = 10;   // 1.6 x 0.9
	public int count160x100 = 10;   // 1.6 x 1.0
	public int count090x160 = 10;   // 0.9 x 1.6
	public int count100x160 = 10;   // 1.0 x 1.6

	[Header("区域限制（仅当 UseWholeSphere = false 时生效）")]
	public Vector2 longitudeRange = new Vector2(-180, 180);  // 经度范围（度）
	public Vector2 latitudeRange = new Vector2(-90, 90);     // 纬度范围（度）

	[Header("是否使用整个球面")]
	public bool useWholeSphere = true;

	[ContextMenu("Generate")]
	void Generate()
	{
		// 清除之前生成的子物体
		while (transform.childCount > 0)
			DestroyImmediate(transform.GetChild(0).gameObject);

		// 收集所有矩形规格
		List<RectSpec> specs = new List<RectSpec>();
		AddSpecs(specs, 1.6f, 0.9f, count160x090);
		AddSpecs(specs, 1.6f, 1.0f, count160x100);
		AddSpecs(specs, 0.9f, 1.6f, count090x160);
		AddSpecs(specs, 1.0f, 1.6f, count100x160);

		// 按对角线长度降序排列（大矩形优先放置）
		specs.Sort((a, b) => b.diag.CompareTo(a.diag));

		// 已放置的矩形信息
		List<PlacedRect> placed = new List<PlacedRect>();

		int maxAttemptsPerRect = 200;
		int totalAttempts = 0;

		foreach (var spec in specs)
		{
			bool placedOk = false;
			for (int attempt = 0; attempt < maxAttemptsPerRect; attempt++)
			{
				totalAttempts++;

				// 生成随机球面坐标（经纬度）
				Vector2 latLon = GetRandomLatLon();
				Vector3 pos = LatLonToPosition(latLon, radius);

				// 检查与已放置矩形是否重叠（基于外接圆半径的球面角距）
				bool conflict = false;
				foreach (var p in placed)
				{
					float angularDist = Vector3.Angle(pos.normalized, p.position.normalized) * Mathf.Deg2Rad;
					float minAngularDist = (spec.diag * 0.5f + p.diag * 0.5f) / radius;
					if (angularDist < minAngularDist)
					{
						conflict = true;
						break;
					}
				}

				if (!conflict)
				{
					CreateRect(pos, spec.width, spec.height);
					placed.Add(new PlacedRect { position = pos, diag = spec.diag });
					placedOk = true;
					break;
				}
			}

			if (!placedOk)
			{
				Debug.LogWarning($"无法放置尺寸 {spec.width:F1}×{spec.height:F1} 的矩形（尝试 {maxAttemptsPerRect} 次后失败）");
			}
		}

		Debug.Log($"总尝试次数: {totalAttempts}");
	}

	void AddSpecs(List<RectSpec> list, float w, float h, int count)
	{
		for (int i = 0; i < count; i++)
			list.Add(new RectSpec { width = w, height = h, diag = Mathf.Sqrt(w * w + h * h) });
	}

	Vector2 GetRandomLatLon()
	{
		if (useWholeSphere)
		{
			// 球面均匀分布（避免极点聚集）
			float u = Random.value;
			float v = Random.value;
			float lon = 2 * Mathf.PI * u - Mathf.PI;
			float lat = Mathf.Asin(2 * v - 1);
			return new Vector2(lat * Mathf.Rad2Deg, lon * Mathf.Rad2Deg);
		} else
		{
			float lon = Random.Range(longitudeRange.x, longitudeRange.y) * Mathf.Deg2Rad;
			float lat = Random.Range(latitudeRange.x, latitudeRange.y) * Mathf.Deg2Rad;
			return new Vector2(lat * Mathf.Rad2Deg, lon * Mathf.Rad2Deg);
		}
	}

	Vector3 LatLonToPosition(Vector2 latLonDeg, float r)
	{
		float lat = latLonDeg.x * Mathf.Deg2Rad;
		float lon = latLonDeg.y * Mathf.Deg2Rad;
		float x = r * Mathf.Cos(lat) * Mathf.Cos(lon);
		float y = r * Mathf.Sin(lat);
		float z = r * Mathf.Cos(lat) * Mathf.Sin(lon);
		return new Vector3(x, y, z);
	}

	void CreateRect(Vector3 center, float width, float height)
	{
		GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
		quad.transform.SetParent(this.transform);
		quad.transform.position = center;
		quad.transform.localScale = new Vector3(width, height, 1);

		// 构建朝向：Z轴指向球外，X轴沿纬度线（东向）
		Vector3 radialOut = center.normalized;               // 球心→表面（Z轴方向）
		Vector3 east = Vector3.Cross(Vector3.up, radialOut).normalized; // 纬度线切线（东）
		if (east.magnitude < 0.001f)                         // 极点附近退化为任意水平方向
			east = Vector3.right;
		Vector3 north = Vector3.Cross(radialOut, east).normalized;      // 经线切线（北）

		Quaternion q = Quaternion.LookRotation(radialOut, north);
		quad.transform.rotation = q;

		// 注意：不再进行任何额外旋转，以保证X轴严格沿纬度线

		// 移除碰撞体（避免干扰）
		DestroyImmediate(quad.GetComponent<Collider>());
	}

	struct RectSpec
	{
		public float width, height, diag;
	}

	struct PlacedRect
	{
		public Vector3 position;
		public float diag;
	}
}