using UnityEngine;
using System.Collections;

namespace Project.Lib {

	/// <summary>
	/// 遮蔽オブジェクト
	/// </summary>
	public class Occluder{
		//頂点
		public Vector3[] point;
		public const int OCCLUDER_POINT = 4;

		//遮蔽計算用のテンポラリ
		//ビュー変換後の座標
		public Vector3[] cameraPoint;
		//前面の平面情報
		public ShapeCollision.Plane front;
		//側面の法線情報
		public Vector3[] normal;

		//もっともカメラに近い座標のz値
		public float nearZ;


		public Occluder(){
			point = new Vector3[OCCLUDER_POINT];
			cameraPoint = new Vector3[OCCLUDER_POINT];
			normal = new Vector3[OCCLUDER_POINT];
		}

		/// <summary>
		/// 頂点をビュー変換してテンポラリに入れる
		/// </summary>
		public void CalcViewMatrix(Matrix4x4 matrix){
			for (int i = 0; i < OCCLUDER_POINT; i++) {
				cameraPoint [i] = (matrix.MultiplyPoint3x4 (point [i]));
			}
		}

		/// <summary>
		/// 遮蔽平面面を計算
		/// </summary>
		public void calcOcclusionPlane()
		{
			//まずは前面を計算
			front = CollisionUtility.calcPlaneFromTriangle(cameraPoint[0], cameraPoint[1], cameraPoint[2]);
			//dの値から遮蔽オブジェクトが表か裏かを調べる
			//原点と平面の距離=-dなので、dがプラスの場合は裏、マイナスの場合は表
			//表の場合
			if(front.d > 0)
			{
				for (int i = 0; i < OCCLUDER_POINT - 1; i++) {
					normal[i] = Vector3.Cross(cameraPoint[i], cameraPoint[i + 1]);
				}
				normal[OCCLUDER_POINT - 1] = Vector3.Cross(cameraPoint[OCCLUDER_POINT - 1], cameraPoint[0]);

				//normal[0] = Vector3.Cross(cameraPoint[0], cameraPoint[1]);
				//normal[1] = Vector3.Cross(cameraPoint[1], cameraPoint[2]);
				//normal[2] = Vector3.Cross(cameraPoint[2], cameraPoint[3]);
				//normal[3] = Vector3.Cross(cameraPoint[3], cameraPoint[0]);
			}
			//裏の場合
			else
			{
				for (int i = 0; i < OCCLUDER_POINT - 1; i++) {
					normal[i] = Vector3.Cross(cameraPoint[i + 1], cameraPoint[i]);
				}
				normal[OCCLUDER_POINT - 1] = Vector3.Cross(cameraPoint[0], cameraPoint[OCCLUDER_POINT - 1]);

				//normal[0] = Vector3.Cross(cameraPoint[1], cameraPoint[0]);
				//normal[1] = Vector3.Cross(cameraPoint[2], cameraPoint[1]);
				//normal[2] = Vector3.Cross(cameraPoint[3], cameraPoint[2]);
				//normal[3] = Vector3.Cross(cameraPoint[0], cameraPoint[3]);

				//法線ベクトルをを反転
				front.normal.x = -front.normal.x;
				front.normal.y = -front.normal.y;
				front.normal.z = -front.normal.z;
			}
			//法線を正規化
			for(int i = 0; i < OCCLUDER_POINT; i++)
			{
				normal[i].Normalize();
			}
			front.normal.Normalize();

		}



		/// <summary>
		/// 一番手前にあるz値を計算
		/// </summary>
		public void CalcNearZ(){
			nearZ = cameraPoint [0].z;
			for (int i = 1; i < OCCLUDER_POINT; i++) {
				if (cameraPoint [i].z > nearZ) {
					nearZ = cameraPoint [i].z;
				}
			}
		}


#if UNITY_EDITOR
		public void DrawGizmos() {
			for (int i = 0; i < OCCLUDER_POINT - 1; i++) {
				Gizmos.DrawLine (point [i], point [i + 1]);
			}
			Gizmos.DrawLine (point [OCCLUDER_POINT - 1], point [0]);
		}
#endif
	}


	/// <summary>
	/// 境界BOX(軸並行境界BOXのmin,maxバージョン)
	/// </summary>
	public class Bounding{
		//境界の最小座標
		public Vector3 min;
		//境界の最大座標
		public Vector3 max;
#if UNITY_EDITOR
		public void DrawGizmos() {
			Vector3 size = max - min;
			Gizmos.DrawWireCube (min + (size * 0.5f), new Vector3 (Mathf.Abs (size.x), Mathf.Abs (size.y), Mathf.Abs (size.z)));
		}
#endif

		/// <summary>
		/// 軸並行境界BOXと軸並行境界BOXの交差を判定する
		/// </summary>
		public static bool Detect(Bounding alpha, Bounding bravo)
		{
			//負荷軽減の可能性を考慮してy軸計算は最後にする
			if (alpha.min.x > bravo.max.x) return false;
			if (alpha.max.x < bravo.min.x) return false;
			if (alpha.min.z > bravo.max.z) return false;
			if (alpha.max.z < bravo.min.z) return false;
			if (alpha.min.y > bravo.max.y) return false;
			if (alpha.max.y < bravo.min.y) return false;
			return true;
		}
	}



	/// <summary>
	/// コリジョンの形状定義
	/// </summary>
	public static class ShapeCollision{
		/// <summary>
		/// 球
		/// </summary>
		public class Sphere
		{
			//中心座標(ベクトル)
			public Vector3		center;
			//半径(スカラー)
			public float 		radius;
#if UNITY_EDITOR
			public void DrawGizmos() {
				Gizmos.DrawWireSphere (center, radius);
			}
#endif
		}
		/// <summary>
		/// 三角形
		/// </summary>
		public class Triangle
		{
			//三角形の頂点データ
			public Vector3[] point = new Vector3[3];
#if UNITY_EDITOR
			public void DrawGizmos() {
				Gizmos.DrawLine (point[0], point[1]);
				Gizmos.DrawLine (point[0], point[2]);
				Gizmos.DrawLine (point[1], point[2]);
			}
#endif
		}
		/// <summary>
		/// 平面
		/// </summary>
		public class Plane
		{
			//法線方向
			public Vector3 	normal;
			//原点と平面状の任意の点と法線ベクトルの内積
			public float 		d;
		}
		/// <summary>
		/// 直線
		/// </summary>
		public class Ray
		{
			//直線のベクトル
			public Vector3 vector;
			//直線上の任意の点
			public Vector3 point;
#if UNITY_EDITOR
			public void DrawGizmos() {
				Gizmos.DrawRay (new UnityEngine.Ray (point, vector));
			}
#endif
		}
		/// <summary>
		/// 線分
		/// </summary>
		public class Segment
		{
			//線分の端点
			public Vector3[] 			point = new Vector3[2];
#if UNITY_EDITOR
			public void DrawGizmos() {
				Gizmos.DrawLine (point[0], point[1]);
			}
#endif
		}

		/// <summary>
		/// カプセル
		/// </summary>
		public class Capsule
		{
			//線分の端点
			public Vector3[] 			point = new Vector3[2];
			public float 				radius;
#if UNITY_EDITOR
			//分割数
			const int DivMax = 10;
			const int DivSimple = 4;
			const int DivDetail = 12;
			//中間点を入れるテンポラリ
			Vector3[] divPoint = new Vector3 [DivMax + 1];
			public void DrawGizmos() {
				//カプセルの上部中心と下部中心座標が一致していたら球として表示
				if (point [0] == point [1]) {
					Gizmos.DrawWireSphere (point [0], radius);
					return;
				}



				Vector3 axis = (point [0] - point [1]).normalized * radius;
				Vector3 vertical = (Vector3.Cross (axis, axis + Vector3.one)).normalized * radius;

				//半球描画
				{
					for (int i = 0; i <= DivMax; i++) {
						float t = (float)i / DivMax;
						divPoint [i] = (axis * t + vertical * (1f - t)).normalized * radius;
					} 

					for (int count = 0; count < DivDetail; count++) {
						for (int i = 0; i < DivMax; i++) {
							divPoint [i] = Quaternion.AngleAxis (360f / DivDetail, axis) * divPoint [i];
						}
						for (int i = 0; i < DivMax; i++) {
							Gizmos.DrawLine (divPoint [i] + point[0], divPoint [i + 1] + point[0]);
						}
					}
				}
				//逆の半球描画
				{
					for (int i = 0; i <= DivMax; i++) {
						float t = (float)i / DivMax;
						divPoint [i] = (-axis * t + vertical * (1f - t)).normalized * radius;
					} 

					for (int count = 0; count < DivDetail; count++) {
						for (int i = 0; i < DivMax; i++) {
							divPoint [i] = Quaternion.AngleAxis (360f / DivDetail, axis) * divPoint [i];
						}
						for (int i = 0; i < DivMax; i++) {
							Gizmos.DrawLine (divPoint [i] + point[1], divPoint [i + 1] + point[1]);
						}
					}
				}
				//胴の円柱部を描画
				{
					for (int count = 0; count < DivDetail; count++) {
						Vector3 p = Quaternion.AngleAxis (360f / DivDetail * count, axis) * vertical;
						Gizmos.DrawLine (p + point [0], p + point [1]);

					}
					Vector3 p0 = vertical;
					for (int i = 1, max = DivMax * 4; i <= max; i++) {
						Vector3 p1 = Quaternion.AngleAxis ((360f / max) * i, axis) * vertical;
						Gizmos.DrawLine (p0 + point[0], p1 + point[0]);
						Gizmos.DrawLine (p0 + point[1], p1 + point[1]);


						p0 = p1;
					}

				}

			}
#endif
		}
		/// <summary>
		/// 軸並行境界BOX
		/// </summary>
		public class AABB
		{
			//中心の座標
			public Vector3 center;
			//各軸の半径
			public float[] radius = new float[3];
#if UNITY_EDITOR
			public void DrawGizmos() {
				Gizmos.DrawWireCube (center, new Vector3(radius[0]*2f, radius[1]*2f,radius[2]*2f));
			}
#endif
		}
		/// <summary>
		/// 有向境界BOX
		/// </summary>
		[System.Serializable]
		public class OBB
		{
			//中心の座標
			public Vector3 center;
			//ローカル座標系(正規化する必要有り)
			public Vector3[] axis = new Vector3[3];
			//各軸の半径
			public float[] 	radius = new float[3];
#if UNITY_EDITOR
			public void DrawGizmos() {
				Vector3 v0 = axis [0] * radius [0];
				Vector3 v1 = axis [1] * radius [1];
				Vector3 v2 = axis [2] * radius [2];

				Gizmos.DrawLine (center + v0 + v1 + v2, center + v0 + v1 - v2);
				Gizmos.DrawLine (center + v0 + v1 + v2, center + v0 - v1 + v2);
				Gizmos.DrawLine (center + v0 - v1 - v2, center + v0 + v1 - v2);
				Gizmos.DrawLine (center + v0 - v1 - v2, center + v0 - v1 + v2);

				Gizmos.DrawLine (center - v0 + v1 + v2, center - v0 + v1 - v2);
				Gizmos.DrawLine (center - v0 + v1 + v2, center - v0 - v1 + v2);
				Gizmos.DrawLine (center - v0 - v1 - v2, center - v0 + v1 - v2);
				Gizmos.DrawLine (center - v0 - v1 - v2, center - v0 - v1 + v2);

				Gizmos.DrawLine (center + v0 + v1 + v2, center - v0 + v1 + v2);
				Gizmos.DrawLine (center + v0 + v1 - v2, center - v0 + v1 - v2);
				Gizmos.DrawLine (center + v0 - v1 + v2, center - v0 - v1 + v2);
				Gizmos.DrawLine (center + v0 - v1 - v2, center - v0 - v1 - v2);
			}
#endif
		}
		/// <summary>
		/// 四辺形(OBBを1次元減少)
		/// </summary>
		public class Quad
		{
			//中心の座標
			public Vector3 center;
			//ローカル座標系(正規化する必要有り)
			public Vector3[] axis = new Vector3[2];
			//各軸の半径
			public float[] radius = new float[2];
		}
	}



	/// <summary>
	/// コリジョンの交差判定
	/// </summary>
	public static class DetectCollision {
		/// <summary>
		/// 球と球の衝突判定を行う
		/// </summary>
		public static bool detectSphereSphere(ShapeCollision.Sphere alpha, ShapeCollision.Sphere bravo)
		{
			Vector3 vec;
			vec = alpha.center - bravo.center;
			float r = alpha.radius + bravo.radius;

			//距離の2乗を比較する
			return vec.sqrMagnitude < (r * r);
		}

		/// <summary>
		/// 点とXZ平面との衝突判定を行う
		/// </summary>
		public static bool detectPointQuad(Vector3 pos,ShapeCollision.Quad quad)
		{
			float left = quad.center.x - quad.radius [0];
			float right = quad.center.x + quad.radius [0];
			float top = quad.center.z + quad.radius [1];
			float bottom = quad.center.z - quad.radius [1];

			if ((left <= pos.x && pos.x <= right) &&
			   (bottom <= pos.z && pos.z <= top)) {
				return true;
			}
			return false;
		}

		/// <summary>
		/// 球とAABBの衝突判定を行う
		/// </summary>
		public static bool detectSphereAABB(ShapeCollision.Sphere sphere, ShapeCollision.AABB aabb)
		{
			//AABBの球の中心に対する最近接点を求める
			Vector3 pos = CollisionUtility.calcClosestPoint(sphere.center, aabb);

			//最近接点と球の中心の距離を求める
			Vector3 vec = pos - sphere.center;

			return vec.sqrMagnitude < (sphere.radius * sphere.radius);
		}
		/// <summary>
		/// 球とOBBの衝突判定を行う
		/// </summary>
		public static bool detectSphereOBB(ShapeCollision.Sphere sphere, ShapeCollision.OBB obb)
		{
			//OBBの球の中心に対する最近接点を求める
			Vector3 pos = CollisionUtility.calcClosestPoint(sphere.center, obb);

			//最近接点と球の中心の距離を求める
			//Vector3 vec = pos - sphere.center;
			//return vec.sqrMagnitude < (sphere.radius * sphere.radius);
			pos.x -= sphere.center.x;
			pos.y -= sphere.center.x;
			pos.z -= sphere.center.x;

			return pos.sqrMagnitude < (sphere.radius * sphere.radius);
		}

		/// <summary>
		/// 球とカプセルの衝突判定を行う
		/// </summary>
		public static bool detectSphereCapsule(ShapeCollision.Sphere sphere, ShapeCollision.Capsule capsule)
		{
			//球の中心とカプセルの線分の距離を求める
			ShapeCollision.Segment seg = new ShapeCollision.Segment();
			seg.point [0] = capsule.point [0];
			seg.point[1] = capsule.point[1];
			float distSq = CollisionUtility.calcPointSegmentDistanceSq(sphere.center, seg);
			//球の半径とカプセルの半径の合計
			float r = sphere.radius + capsule.radius;

			//距離が半径の合計よりも小さい場合は衝突している
			return distSq <= r * r;
		}



		/// <summary>
		/// 軸並行境界BOXと軸並行境界BOXの交差を判定する
		/// </summary>
		public static bool detectAABBAABB(ShapeCollision.AABB alpha, ShapeCollision.AABB bravo)
		{
			//負荷軽減の可能性を考慮してy軸計算は最後にする
			if(Mathf.Abs(alpha.center.x - bravo.center.x) > alpha.radius[0] + bravo.radius[0]) return false;
			if(Mathf.Abs(alpha.center.z - bravo.center.z) > alpha.radius[2] + bravo.radius[2]) return false;
			if(Mathf.Abs(alpha.center.y - bravo.center.y) > alpha.radius[1] + bravo.radius[1]) return false;

			return true;
		}

		/// <summary>
		/// 軸並行境界BOXと有向境界BOXの交差を判定する
		/// </summary>
		public static bool detectAABBOBB(ShapeCollision.AABB alpha, ShapeCollision.OBB bravo)
		{
			//AABBをOBBにしてOBBとOBBの衝突として計算する
			ShapeCollision.OBB obb = new ShapeCollision.OBB();
			obb.center = alpha.center;
			obb.axis[0].x = 1f;
			obb.axis[1].y = 1f;
			obb.axis[2].z = 1f;
			obb.radius [0] = alpha.radius [0];
			obb.radius [1] = alpha.radius [1];
			obb.radius [2] = alpha.radius [2];

			return detectOBBOBB(obb, bravo);
		}

		//@note 中でnewすると無駄にメモリ使うのであらかじめ領域を確保してしまう。c++でやると逆にキャッシュに乗らなくて遅くなるかもしれないのでc#でやるとき限定。
		//detectOBBOBBで使用するテンポラリ
		static float[,] R = new float[4, 4];
		static float[,] AbsR = new float[4, 4];
		static float[] trans = new float[3];


		/// <summary>
		/// 有向境界BOXと有向境界BOXの交差を判定する
		/// </summary>
		public static bool detectOBBOBB(ShapeCollision.OBB alpha, ShapeCollision.OBB bravo)
		{
			float ra, rb;
			float ret;
//			Matrix4x4 R = new Matrix4x4();
//			Matrix4x4 AbsR = new Matrix4x4();

			//alphaの座標系でbravoを表現する回転行列を作成
			for(int i = 0; i < 3; i++)
			{
				for(int j = 0; j < 3; j++)
				{
					//R[i, j] = Vector3.Dot(alpha.axis[i], bravo.axis[j]);
					R[i, j] = alpha.axis[i].x * bravo.axis[j].x + alpha.axis[i].y * bravo.axis[j].y + alpha.axis[i].z * bravo.axis[j].z;
				}
			}

			//平行移動ベクトルを計算
			//Vector3 vec;
//			float[] trans = new float[3];
			//vec = bravo.center - alpha.center;
			Vector3 vec = bravo.center;
			vec.x -= alpha.center.x;
			vec.y -= alpha.center.y;
			vec.z -= alpha.center.z;

			//平行移動ベクトルをalphaの座標系に変換
			//trans[0] = Vector3.Dot(vec, alpha.axis[0]);
			//trans[1] = Vector3.Dot(vec, alpha.axis[1]);
			//trans[2] = Vector3.Dot(vec, alpha.axis[2]);
			trans[0] = vec.x * alpha.axis[0].x + vec.y * alpha.axis[0].y + vec.z * alpha.axis[0].z;
			trans[1] = vec.x * alpha.axis[1].x + vec.y * alpha.axis[1].y + vec.z * alpha.axis[1].z;
			trans[2] = vec.x * alpha.axis[2].x + vec.y * alpha.axis[2].y + vec.z * alpha.axis[2].z;



			//2つの辺が平行でそれらの外積がゼロベクトル（あるいはそれに近いベクトル）になる時に
			//演算エラーが起きないようにイプシロンの項を追加
			for(int i = 0; i < 3; i++)
			{
				for(int j = 0; j < 3; j++)
				{
					if (R [i, j] >= 0f) {
						AbsR [i, j] = R [i, j] + CollisionUtility.EPSILON;
					} else {
						AbsR [i, j] = -R [i, j] + CollisionUtility.EPSILON;
					}
				}
			}

			//固定小数の乗算は計算後、固定小数分シフトすることになるので
			//整数の乗算として扱って最後に固定小数分シフトした方が早くなるかも
			//軸L = A0, L = A1, L = A2を判定
			for(int i = 0; i < 3; i++)
			{
				ra = alpha.radius[i];
				rb = (bravo.radius[0] * AbsR[i, 0]) + (bravo.radius[1] * AbsR[i, 1]) + (bravo.radius[2] * AbsR[i, 2]);
				if (trans [i] >= 0f) {
					if (trans [i] > ra + rb) {
						return false;
					}
				} else {
					if (-trans [i] > ra + rb) {
						return false;
					}
				}

			}

			//軸L = B0, L = B1, L = B2を判定
			for(int i = 0; i < 3; i++)
			{
				ra = (alpha.radius[0] * AbsR[0, i]) + (alpha.radius[1] * AbsR[1, i]) + (alpha.radius[2] * AbsR[2, i]);
				rb = bravo.radius[i];
				ret = (trans [0] * R [0, i]) + (trans [1] * R [1, i]) + (trans [2] * R [2, i]);
				if (ret >= 0f) {
					if (ret > ra + rb) {
						return false;
					}
				} else {
					if (-ret > ra + rb) {
						return false;
					}
				}
			}
			//軸L = A0×B0を判定
			ra = (alpha.radius[1] * AbsR[2, 0]) + (alpha.radius[2] * AbsR[1, 0]);
			rb = (bravo.radius[1] * AbsR[0, 2]) + (bravo.radius[2] * AbsR[0, 1]);
			ret = (trans [2] * R [1, 0]) - (trans [1] * R [2, 0]);
			if (ret >= 0f) {
				if (ret> ra + rb) {
					return false;
				}
			} else {
				if (-ret > ra + rb) {
					return false;
				}
			}

			//軸L = A0×B1を判定
			ra = (alpha.radius[1] * AbsR[2, 1]) + (alpha.radius[2] * AbsR[1, 1]);
			rb = (bravo.radius[0] * AbsR[0, 2]) + (bravo.radius[2] * AbsR[0, 0]);
			ret = (trans[2] * R[1, 1]) - (trans[1] * R[2, 1]);
			if (ret >= 0f) {
				if (ret > ra + rb) {
					return false;
				}
			} else {
				if (-ret > ra + rb) {
					return false;
				}
			}

			//軸L = A0×B2を判定
			ra = (alpha.radius[1] * AbsR[2, 2]) + (alpha.radius[2] * AbsR[1, 2]);
			rb = (bravo.radius[0] * AbsR[0, 1]) + (bravo.radius[1] * AbsR[0, 0]);
			ret = (trans[2] * R[1, 2]) - (trans[1] * R[2, 2]);
			if (ret >= 0f) {
				if (ret > ra + rb) {
					return false;
				}
			} else {
				if (-ret > ra + rb) {
					return false;
				}
			}

			//軸L = A1×B0を判定
			ra = (alpha.radius[0] * AbsR[2, 0]) + (alpha.radius[2] * AbsR[0, 0]);
			rb = (bravo.radius[1] * AbsR[1, 2]) + (bravo.radius[2] * AbsR[1, 1]);
			ret = (trans [0] * R [2, 0]) - (trans [2] * R [0, 0]);
			if (ret >= 0f) {
				if (ret > ra + rb) {
					return false;
				}
			} else {
				if (-ret > ra + rb) {
					return false;
				}
			}

			//軸L = A1×B1を判定
			ra = (alpha.radius[0] * AbsR[2, 1]) + (alpha.radius[2] * AbsR[0, 1]);
			rb = (bravo.radius[0] * AbsR[1, 2]) + (bravo.radius[2] * AbsR[1, 0]);
			ret = (trans[0] * R[2, 1]) - (trans[2] * R[0, 1]);
			if (ret >= 0f) {
				if (ret > ra + rb) {
					return false;
				}
			} else {
				if (-ret > ra + rb) {
					return false;
				}
			}

			//軸L = A1×B2を判定
			ra = (alpha.radius[0] * AbsR[2, 2]) + (alpha.radius[2] * AbsR[0, 2]);
			rb = (bravo.radius[0] * AbsR[1, 1]) + (bravo.radius[1] * AbsR[1, 0]);
			ret = (trans [0] * R [2, 2]) - (trans [2] * R [0, 2]);
			if (ret >= 0f) {
				if (ret > ra + rb) {
					return false;
				}
			} else {
				if (-ret > ra + rb) {
					return false;
				}
			}

			//軸L = A2×B0を判定
			ra = (alpha.radius[0] * AbsR[1, 0]) + (alpha.radius[1] * AbsR[0, 0]);
			rb = (bravo.radius[1] * AbsR[2, 2]) + (bravo.radius[2] * AbsR[2, 1]);
			ret = (trans[1] * R[0, 0]) - (trans[0] * R[1, 0]);
			if (ret >= 0f) {
				if (ret > ra + rb) {
					return false;
				}
			} else {
				if (-ret > ra + rb) {
					return false;
				}
			}

			//軸L = A2×B1を判定
			ra = (alpha.radius[0] * AbsR[1, 1]) + (alpha.radius[1] * AbsR[0, 1]);
			rb = (bravo.radius[0] * AbsR[2, 2]) + (bravo.radius[2] * AbsR[2, 0]);
			ret = (trans [1] * R [0, 1]) - (trans [0] * R [1, 1]);
			if (ret >= 0f) {
				if (ret > ra + rb) {
					return false;
				}
			} else {
				if (-ret > ra + rb) {
					return false;
				}
			}

			//軸L = A2×B2を判定
			ra = (alpha.radius[0] * AbsR[1, 2]) + (alpha.radius[1] * AbsR[0, 2]);
			rb = (bravo.radius[0] * AbsR[2, 1]) + (bravo.radius[1] * AbsR[2, 0]);
			ret = (trans [1] * R [0, 2]) - (trans [0] * R [1, 2]);
			if (ret >= 0f) {
				if (ret > ra + rb) {
					return false;
				}
			} else {
				if (-ret > ra + rb) {
					return false;
				}
			}

			return true;

		}

		/// <summary>
		/// 線分とAABBの黄交差を判定
		/// </summary>
		public static bool detectSegmentAABB(Vector3 p0, Vector3 p1, Vector3 min, Vector3 max)
		{
			Vector3 c = (min + max) * 0.5f;		//ボックスの中心点
			Vector3 e = max - c;				//bックスの幅の半分の範囲
			Vector3 m = (p0 + p1) * 0.5f;		//線分の中点
			Vector3 d = p1 - m;					//線分の長さの半分のベクトル
			m = m - c;							//ボックスと線分を原点まで平行移動

			//ワールド座標軸が分離軸か試す
			float adx = Mathf.Abs(d.x);
			if (Mathf.Abs (m.x) > e.x + adx) {
				return false;
			}

			float ady = Mathf.Abs(d.y);
			if (Mathf.Abs (m.y) > e.y + ady) {
				return false;
			}

			float adz = Mathf.Abs(d.z);
			if (Mathf.Abs (m.z) > e.z + adz) {
				return false;
			}


			//線分が座標軸に平行(あるいはそれに近い状態)のときにイプシロンを追加して計算誤差の影響を弱める
			adx += CollisionUtility.EPSILON;
			ady += CollisionUtility.EPSILON;
			adz += CollisionUtility.EPSILON;

			//線分の方向ベクトルの外せきを座標軸に対して試す
			if (Mathf.Abs (m.y * d.z - m.z * d.y) > e.y * adz + e.z * ady)
				return false;
			if (Mathf.Abs (m.z * d.x - m.x * d.z) > e.x * adz + e.z * adx)
				return false;
			if (Mathf.Abs (m.x * d.y - m.y * d.x) > e.x * ady + e.y * adx)
				return false;
			


			return true;
		}
		//@note この計算は不完全だった。カプセル=線分にボリュームをつけたものと考えて半径をboxの方に足して計算しようとしたが、単純にboxの半径にカプセルの半径を載せると辺や角で計算が甘くなる
		/// <summary>
		/// 線分とOBBの黄交差を判定
		/// </summary>
		public static bool detectCapsuleOBB(ShapeCollision.Capsule capsule, ShapeCollision.OBB obb)
		{
			Vector3 v;
			v = capsule.point [0] - obb.center;
			Vector3 p0 = new Vector3(Vector3.Dot(v, obb.axis[0]), Vector3.Dot(v, obb.axis[1]), Vector3.Dot(v, obb.axis[2]));

			v = capsule.point [1] - obb.center;
			Vector3 p1 = new Vector3(Vector3.Dot(v, obb.axis[0]), Vector3.Dot(v, obb.axis[1]), Vector3.Dot(v, obb.axis[2]));

			Vector3 max = new Vector3 (obb.radius [0] + capsule.radius, obb.radius [1] + capsule.radius, obb.radius [2] + capsule.radius);
			Vector3 min = -max;

			return detectSegmentAABB(p0, p1, min, max);
		}
		/// <summary>
		/// 光線と球の交差を判定する
		/// </summary>
		/// <remarks>
		/// 交差する時間と交差点が得られる
		/// </remarks>
		public static bool detectRaySphere(ShapeCollision.Ray ray, ShapeCollision.Sphere sphere, ref float t, ref Vector3 q)
		{
			Vector3 m = ray.point - sphere.center;
			float b = Vector3.Dot(m, ray.vector);
			float c = Vector3.Dot(m, m) - sphere.radius * sphere.radius;

			//離れていく方向を指す場合は終了
			if(c > 0f && b > 0f) return false;

			float discr = b * b - c;
			//負の場合は光線が球を外れている
			if(discr < 0f) return false;

			//交差はするので交差する最小の値tを計算
			t = -b - Mathf.Sqrt(discr);

			//tが負の場合、光線は球の内側から始まっている
			if(t < 0f) t = 0f;
			q = ray.point + ray.vector * t;
			return true;
		}
		/// <summary>
		/// 移動している球同士の交差を判定する
		/// </summary>
		/// <remarks>
		/// 交差する時間が得られる
		/// </remarks>
		public static bool detectMovingSphereSphere(ShapeCollision.Sphere alpha, ShapeCollision.Sphere bravo, Vector3 va, Vector3 vb, ref float t)
		{
			//球bravoをalphaの半径分拡張した球を定義
			ShapeCollision.Sphere s1 = new ShapeCollision.Sphere();
			s1.center = bravo.center;
			s1.radius = bravo.radius + alpha.radius;
			//alphaを静止させるベクトルを計算
			Vector3 v = va - vb;


			float vlen = v.magnitude;

			Vector3 q = new Vector3();
			ShapeCollision.Ray ray = new ShapeCollision.Ray();
			ray.point = alpha.center;
			ray.vector = v / vlen;
			if(detectRaySphere(ray, s1, ref t, ref q))
			{
				return t <= vlen ? true : false;
			}

			return false;
		}
		/// <summary>
		/// 移動しているAABB同士の交差を判定する
		/// </summary>
		/// <remarks>
		/// 交差する場合は接触開始時刻と終了時刻が得られる
		/// </remarks>
		public static bool detectMovingAABBAABB(ShapeCollision.AABB alpha, ShapeCollision.AABB bravo, Vector3 va, Vector3 vb, ref float tfirst, ref float tlast)
		{
			if(detectAABBAABB(alpha, bravo))
			{
				tfirst = tlast = 0f;
				return true;
			}
			//相対速度を計算し、alphaは静止しているものとして扱う
			Vector3 v = vb - va;
			//相対速度0の場合は衝突しない
			if(v.x == 0f &&  v.y == 0f && v.z == 0f)
				return false;

			tfirst = 0f;
			tlast = 1f;

			bool result;
			//x軸判定
			result = calcMovinAABBAABB(alpha.center.x + alpha.radius[0], alpha.center.x - alpha.radius[0], 
				bravo.center.x + bravo.radius[0], bravo.center.x - bravo.radius[0],
				v.x, ref tfirst, ref tlast);
			if(!result) return false; 
			if(tfirst > tlast) return false;
			//y軸判定
			result = calcMovinAABBAABB(alpha.center.y + alpha.radius[1], alpha.center.y - alpha.radius[1], 
				bravo.center.y + bravo.radius[1], bravo.center.y - bravo.radius[1],
				v.y, ref tfirst, ref tlast);
			if(!result) return false; 
			if(tfirst > tlast) return false;

			//z軸判定
			result = calcMovinAABBAABB(alpha.center.z + alpha.radius[2], alpha.center.z - alpha.radius[2], 
				bravo.center.z + bravo.radius[2], bravo.center.z - bravo.radius[2],
				v.z, ref tfirst, ref tlast);
			if(!result) return false; 
			if(tfirst > tlast) return false;


			return true;
		}


		/// <summary>
		/// 移動しているAABB同士の判定用関数
		/// </summary>
		/// <remarks>
		/// 交差する場合は接触開始時刻と終了時刻が得られる
		/// </remarks>
		public static bool calcMovinAABBAABB(float alphaMax, float alphaMin, float bravoMax, float bravoMin, float v, ref float tfirst, ref float tlast)
		{
			if(v < 0f)
			{
				if(bravoMax < alphaMin) return false;
				if(alphaMax < bravoMin) tfirst = Mathf.Max((alphaMax - bravoMin) / v, tfirst);
				if(bravoMax > alphaMin) tlast = Mathf.Min((alphaMin - bravoMax) / v, tlast);
			}
			else if(v > 0f)
			{
				if(bravoMin > alphaMax) return false;
				if(bravoMax < alphaMin) tfirst = Mathf.Max((alphaMin - bravoMax) / v, tfirst);
				if(alphaMax > bravoMin) tlast = Mathf.Min((alphaMax - bravoMin) / v, tlast);
			}

			return true;
		}
	}

	/// <summary>
	/// コリジョン計算に使う便利関数
	/// </summary>
	public static class CollisionUtility{
		//計算誤差対策。
		public const float EPSILON = 0.000001f;
		/// <summary>
		/// AABBの点に対する最近接点を計算
		/// </summary>
		public static Vector3 calcClosestPoint(Vector3 point, ShapeCollision.AABB aabb)
		{
			float[] pos = new float[3];
			float[] pv = new float[]{point.x, point.y, point.z};
			float[] center = new float[]{aabb.center.x, aabb.center.y, aabb.center.z};
			for(int i = 0; i < 3; i++)
			{
				float v = pv[i];
				if(v < center[i] - aabb.radius[i]) v = center[i] - aabb.radius[i];
				if(v > center[i] + aabb.radius[i]) v = center[i] + aabb.radius[i];

				pos[i] = v;
			}

			return new Vector3(pos[0], pos[1], pos[2]);
		}
		/// <summary>
		/// OBBの点に対する最近接点を計算
		/// </summary>
		public static Vector3 calcClosestPoint(Vector3 point, ShapeCollision.OBB obb)
		{
			Vector3 pos = obb.center;
			//Vector3 vec = point - obb.center;
			Vector3 vec = point;
			vec.x -= obb.center.x;
			vec.y -= obb.center.y;
			vec.z -= obb.center.z;
			for(int i = 0; i < 3; i++)
			{
				float dist = Vector3.Dot(vec, obb.axis[i]);
				if(dist >  obb.radius[i]) dist =  obb.radius[i];
				if(dist < -obb.radius[i]) dist = -obb.radius[i];

				//pos = pos + obb.axis[i] * dist;
				pos.x = pos.x + obb.axis[i].x * dist;
				pos.y = pos.y + obb.axis[i].y * dist;
				pos.z = pos.z + obb.axis[i].z * dist;
			}

			return pos;
		}
		/// <summary>
		/// 点と線分abの距離の平方を計算
		/// </summary>
		public static float calcPointSegmentDistanceSq(Vector3 point, ShapeCollision.Segment segment)
		{
			Vector3 ab = segment.point[1] - segment.point[0];
			Vector3 ac = point - segment.point[0];
			Vector3 bc = point - segment.point[1];

			//点が線分の外側に射影される場合
			float t = Vector3.Dot(ac, ab);
			if(t <= 0f)
				return Vector3.Dot(ac, ac);
			float denom = Vector3.Dot(ab, ab);
			if(t >= denom)
				return Vector3.Dot(bc, bc);

			//線分上に射影される場合
			return Vector3.Dot(ac, ac) - t * t / denom;
		}

		/// <summary>
		/// 点と平面の距離を計算する
		/// </summary>
		public static float calcPointPlaneDistance(Vector3 point, ShapeCollision.Plane plane)
		{
			float t;
			t = Vector3.Dot(plane.normal, point) - plane.d;
			return t;
		}
		/// <summary>
		/// 線分と線分の距離の平方を計算
		/// </summary>
		/// <remarks>
		/// s, tは再近接点が線分上のどこにいるかの値(0〜1)
		/// c1, c2は他方の線分に対しての再近接点
		/// </remarks>
		public static float calcSegmentSegmentDistanceSq(ShapeCollision.Segment segment1, ShapeCollision.Segment segment2, ref float s, ref float t, ref Vector3 c1, ref Vector3 c2)
		{
			Vector3 d1 = segment1.point[0] - segment1.point[1];
			Vector3 d2 = segment2.point[0] - segment2.point[1];
			Vector3 r = segment1.point[0] - segment2.point[0];


			float a = Vector3.Dot(d1, d1);
			float e = Vector3.Dot(d2, d2);
			float f = Vector3.Dot(d2, r);

			//片方あるいは両方の線分が点に縮退しているかチェック
			if(a <= EPSILON && e <= EPSILON)
			{
				s = t = 0f;
				c1 = segment1.point[0];
				c2 = segment2.point[0];

				return Vector3.Dot(c1 - c2, c1 - c2);
			}

			if(a <= EPSILON)
			{
				//最初の線分が点に縮退
				s = 0f;
				t = f / e;
				t = Mathf.Clamp(t, 0f, 1f);
			}
			else
			{
				float c = Vector3.Dot(d1, r);
				//2番目の線分が点に縮退
				if(e <= EPSILON)
				{
					t = 0f;
					s = Mathf.Clamp(-c / a, 0f, 1f);
				}
				//一般的な縮退の場合
				else
				{
					float b = Vector3.Dot(d1, d2);
					float denom = a*e - b*b;

					if(denom != 0f)
					{
						s = Mathf.Clamp((b*f - c*e) / denom, 0f, 1f);
					}
					else
					{
						s = 0f;
					}

					float tnom = (b*s + f);
					if(t < 0f)
					{
						t = 0f;
						s = Mathf.Clamp(-c/a, 0f, 1f);
					}
					else if(t > e)
					{
						t = 1f;
						s = Mathf.Clamp((b - c)/a, 0f, 1f);
					}
					else
					{
						t = tnom / e;
					}

				}
			}


			c1 = segment1.point[0] + d1 * s;
			c2 = segment2.point[0] + d2 * t;

			return Vector3.Dot(c1 - c2, c1 - c2);


		}


		/// <summary>
		/// 三角形が属する平面のデータを作成する
		/// </summary>
		public static ShapeCollision.Plane calcPlaneFromTriangle(Vector3 pos1, Vector3 pos2, Vector3 pos3)
		{
			ShapeCollision.Plane plane = new ShapeCollision.Plane();
			Vector3 vec1, vec2;

			//三角形の辺のベクトルの外積から平面の法線を取得
			vec1 = pos1 - pos2;
			vec2 = pos2 - pos3;
			plane.normal = Vector3.Cross(vec1, vec2);
			//法線を正規化する
			plane.normal.Normalize();
			//平面の方程式を計算する
			plane.d = Vector3.Dot(plane.normal, pos1);

			return plane;
		}
		/// <summary>
		/// 三角形の点に対する最近接点を計算
		/// </summary>
		public static Vector3 calcClosestPoint(Vector3 point, ShapeCollision.Triangle triangle)
		{
			//pointがAの外側にあるかチェック
			Vector3 ab = triangle.point[1] - triangle.point[0];
			Vector3 ac = triangle.point[2] - triangle.point[0];
			Vector3 ap = point - triangle.point[0];

			float d1 = Vector3.Dot(ab, ap);
			float d2 = Vector3.Dot(ac, ap);

			if(d1 <= 0f && d2 <= 0f)
				return triangle.point[0];


			//pointがBの外側にあるかチェック
			Vector3 bp = point - triangle.point[1];
			float d3 = Vector3.Dot(ab, bp);
			float d4 = Vector3.Dot(ac, bp);

			if(d3 >= 0f && d4 <= d3)
				return triangle.point[1];

			//pointがABの辺領域の中にあるかチェック、あればpointのAB上に対する射影を返す
			float vc = d1 * d4 - d3 * d2;
			if(vc <= 0f && d1 >= 0f && d3 <= 0f)
			{
				float v = d1 / (d1 - d3);
				return triangle.point[0] + ab * v;
			}

			//pointがCの外側にあるかチェック
			Vector3 cp = point - triangle.point[2];
			float d5 = Vector3.Dot(ab, cp);
			float d6 = Vector3.Dot(ac, cp);

			if(d6 >= 0f && d5 <= d6)
				return triangle.point[2];

			float vb = d5 * d2 - d1 * d6;
			if(vb <= 0f && d2 >= 0f && d6 <= 0f)
			{
				float w = d2 / (d2 - d6);
				return triangle.point[0] + ac * w;
			}

			float va = d3 * d6 - d5 * d4;
			if(va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f)
			{
				float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
				return triangle.point[1] + (triangle.point[2] - triangle.point[1]) * w;
			}

			{
				float denom = 1f / (va + vb + vc);
				float v = vb * denom;
				float w = vc * denom;
				return triangle.point [0] + ab * v + ac * w;
			}
		}


		/// <summary>
		/// 点が三角形の内側にあるかチェック
		/// </summary>
		public static bool checkInTriangle(Vector3 point, ShapeCollision.Triangle triangle)
		{
			//pointが原点にあるときの三角形を計算
			Vector3 a = triangle.point[0] - point;
			Vector3 b = triangle.point[1] - point;
			Vector3 c = triangle.point[2] - point;
			//三角形pab, pbcの法線を計算
			Vector3 u = Vector3.Cross(a, b);
			Vector3 v = Vector3.Cross(b, c);

			//両方が同じ方向を指していることを確認
			if(Vector3.Dot(u, v) < 0f)
				return false;
			//三角形pcaの法線を計算
			Vector3 w = Vector3.Cross(c, a);

			//両方が同じ方向を指していることを確認
			if(Vector3.Dot(u, w) < 0f)
				return false;

			return true;
		}



	}
}
