using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Project.Lib {
	/// <summary>
	/// 視錐台カリング
	/// </summary>
	public class FrustumCulling {
		
		const int Upper = 0;
		const int Lower = 1;
		const int Left  = 2;
		const int Right = 3;

		//視錐台の上下左右の4面
		ShapeCollision.Plane[] plane_ = new ShapeCollision.Plane[4]{
			new ShapeCollision.Plane(),
			new ShapeCollision.Plane(),
			new ShapeCollision.Plane(),
			new ShapeCollision.Plane(),
		};
		//視錐台のnearとfar
		float near_;
		float far_;

		/// <summary>
		/// 視錐台の状態を更新
		/// </summary>
		/// <remarks>
		/// 座標変換後の視錐台の4平面とnear、farをセット
		/// 透視変換行列に変更があったら実行する(near, far, fov, aspectいずれかが変化した場合に実行が必要)
		/// </remarks>
		public void SetFrustum(float fov, float aspect, float nearClip, float farClip)
		{
			//原点を通るためdは常に0
			//上下面はyz平面に対して垂直なので法線のx軸成分は0
			//左右面はxz平面に対して垂直なので法線のy軸成分は0

			//画角から平面を計算
			float half = fov / 2;
			//上面
			{
				float halfRad = (-half + 90f) * Mathf.Deg2Rad;
				plane_[Upper].normal.x = 0f;
				plane_[Upper].normal.y = Mathf.Sin (halfRad);
				plane_[Upper].normal.z = Mathf.Cos (halfRad);
				plane_[Upper].d = 0f;
			}
			//下面
			{
				float halfRad = (half - 90f) * Mathf.Deg2Rad;
				plane_[Lower].normal.x = 0f;
				plane_[Lower].normal.y = Mathf.Sin(halfRad);
				plane_[Lower].normal.z = Mathf.Cos(halfRad);
				plane_[Lower].d = 0f;
			}


			//アスペクト比をかけた角度で計算
			float half2 = half * aspect;
			//左面
			{
				float halfRad = (half2 - 90f) * Mathf.Deg2Rad;
				plane_[Left].normal.x = Mathf.Sin(halfRad);
				plane_[Left].normal.y = 0f;
				plane_[Left].normal.z = Mathf.Cos(halfRad);
				plane_[Left].d = 0;
			}

			//右面
			{
				float halfRad = (-half2 + 90f) * Mathf.Deg2Rad;
				plane_[Right].normal.x = Mathf.Sin(halfRad);
				plane_[Right].normal.y = 0f;
				plane_[Right].normal.z = Mathf.Cos(halfRad);
				plane_[Right].d = 0;
			}
			//zマイナス方向を向いているのでマイナスを付ける
			near_ = -nearClip;
			far_ = -farClip;

		}

		/// <summary>
		/// 境界球の視錐台カリング
		/// </summary>
		private bool SphereTest(Vector3 center, float radius)
		{
			//視錐台の外に球があるか調べる
			//near値比較
			if(center.z - radius > near_)
			{
				//nearクリップ面よりも前にあるので描画されない
				return true;
			}

			//far値比較
			if(center.z + radius < far_)
			{
				//farクリップ面よりも後ろにあるので描画されない
				return true;
			}

			//点と平面の距離計算のうち、0になるのが判明している部分を予め除いて計算する
			//上面の検査
			if(plane_[Upper].normal.y * center.y + plane_[Upper].normal.z * center.z > radius)
			{
				return true;
			}
			//下面の検査
			if(plane_[Lower].normal.y * center.y + plane_[Lower].normal.z * center.z > radius)
			{
				return true;
			}
			//左面の検査
			if(plane_[Left ].normal.x * center.x + plane_[Left ].normal.z * center.z > radius)
			{
				return true;
			}
			//右面の検査
			if(plane_[Right].normal.x * center.x + plane_[Right].normal.z * center.z > radius)
			{
				return true;
			}

			return false;
		}
		/// <summary>
		/// 境界BOXの視錐台カリング
		/// </summary>
		protected bool BoxTest(Vector3[] vertex, int max)
		{
			int count;

			//8頂点すべてがいづれかひとつの面の外側にある場合はカリングOKと判断できる
			/////////////////////////////////////////////////////////////////////////
			//near値比較
			for(count = 0; count < max; count++)
			{
				if(vertex[count].z < near_)	
					break;
			}
			if(count == max) 
				return true;

			/////////////////////////////////////////////////////////////////////////
			//far値比較
			for(count = 0; count < max; count++)
			{
				if(vertex[count].z > far_) 
					break;
			}
			if(count == max) 
				return true;

			/////////////////////////////////////////////////////////////////////////
			//上面の検査
			for(count = 0; count < max; count++)
			{
				if(plane_[Upper].normal.y * vertex[count].y + plane_[Upper].normal.z * vertex[count].z < 0) 
					break;
			}
			if(count == max) 
				return true;

			/////////////////////////////////////////////////////////////////////////
			//下面の検査
			for(count = 0; count < max; count++)
			{
				if(plane_[Lower].normal.y * vertex[count].y + plane_[Lower].normal.z * vertex[count].z < 0) 
					break;
			}
			if(count == max) 
				return true;

			/////////////////////////////////////////////////////////////////////////
			//左面の検査
			for(count = 0; count < max; count++)
			{
				if(plane_[Left ].normal.x * vertex[count].x + plane_[Left ].normal.z * vertex[count].z < 0) 
					break;
			}
			if(count == max) 
				return true;

			/////////////////////////////////////////////////////////////////////////
			//右面の検査
			for(count = 0; count < max; count++)
			{
				if(plane_[Right].normal.x * vertex[count].x + plane_[Right].normal.z * vertex[count].z < 0)
					break;
			}
			if(count == max) 
				return true;




			return false;
		}

		//newしないためのテンポラリ
		protected Vector3 center_ = new Vector3();
		protected Vector3[] axis_ = new Vector3[3];
		protected Vector3[] vertex_ = new Vector3[8];

		//ビュー行列
		protected Matrix4x4 matrix_;


		/// <summary>
		/// カメラのビュー行列を更新
		/// </summary>
		public virtual void Execute(Matrix4x4 matrix) {
			matrix_ = matrix;
		}

		/// <summary>
		/// 視錐台カリング(境界球)
		/// </summary>
		public virtual bool CullingTest(ShapeCollision.Sphere sphere) {
			center_ = matrix_.MultiplyPoint3x4 (sphere.center);
			return SphereTest (center_, sphere.radius);
		}
		/// <summary>
		/// 視錐台カリング(境界BOX)
		/// </summary>
		public virtual bool CullingTest(ShapeCollision.OBB obb, float circumscribedRadius) {
			return CullingTest(obb.center, obb.axis, obb.radius, circumscribedRadius);
		}

		/// <summary>
		/// 視錐台カリング(境界BOX)
		/// </summary>
		public virtual bool CullingTest(Vector3 center, Vector3[] axis, float[] radius, float circumscribedRadius) {
			//外接球の半径を計算
			circumscribedRadius = Mathf.Sqrt(radius[0] * radius[0] + radius[1] * radius[1] + radius[2] * radius[2]);
			//ボックスはまず外接球でプレ計算をする
			//中心を座標変換する
			center_ = matrix_.MultiplyPoint3x4(center);
			bool result = SphereTest(center_, circumscribedRadius);
			//カリングOKの場合はここで終了
			if(result)
			{
				return true;
			}

			//遮蔽カリングしない場合は内接球が視錐台に含まれるかチェックして
			//含まれる場合はボックステストをせずに描画すると処理が少し軽くなる
			//逆に遮蔽カリングする場合は視錐台の中に入っていても遮蔽されている可能性があるので
			//ここではまだ描画確定できない




			//@note Vectorの計算がびっくりするほど重かったので展開
			/*{
				axis_ [0] = (matrix_.MultiplyVector (obb.axis [0])) * obb.radius [0];
				axis_ [1] = (matrix_.MultiplyVector (obb.axis [1])) * obb.radius [1];
				axis_ [2] = (matrix_.MultiplyVector (obb.axis [2])) * obb.radius [2];



				vertex_ [0] = center_ + axis_ [0] + axis_ [1] + axis_ [2];
				vertex_ [1] = center_ + axis_ [0] + axis_ [1] - axis_ [2];
				vertex_ [2] = center_ + axis_ [0] - axis_ [1] + axis_ [2];
				vertex_ [3] = center_ + axis_ [0] - axis_ [1] - axis_ [2];
				vertex_ [4] = center_ - axis_ [0] + axis_ [1] + axis_ [2];
				vertex_ [5] = center_ - axis_ [0] + axis_ [1] - axis_ [2];
				vertex_ [6] = center_ - axis_ [0] - axis_ [1] + axis_ [2];
				vertex_ [7] = center_ - axis_ [0] - axis_ [1] - axis_ [2];
			}*/
			{
				//OBBの8頂点が全て視錐台の外にあるかチェック
				//中心座標は球と同じものが使えるので計算不要
				//各軸を回転のみ行って半径倍する = OBBの各軸半径ベクトル
				axis_ [0] = (matrix_.MultiplyVector (axis [0]));
				axis_ [1] = (matrix_.MultiplyVector (axis [1]));
				axis_ [2] = (matrix_.MultiplyVector (axis [2]));

				axis_ [0].x = axis_ [0].x * radius [0];
				axis_ [0].y = axis_ [0].y * radius [0];
				axis_ [0].z = axis_ [0].z * radius [0];

				axis_ [1].x = axis_ [1].x * radius [1];
				axis_ [1].y = axis_ [1].y * radius [1];
				axis_ [1].z = axis_ [1].z * radius [1];

				axis_ [2].x = axis_ [2].x * radius [2];
				axis_ [2].y = axis_ [2].y * radius [2];
				axis_ [2].z = axis_ [2].z * radius [2];


				vertex_ [0].x = center_.x + axis_ [0].x + axis_ [1].x + axis_ [2].x;
				vertex_ [0].y = center_.y + axis_ [0].y + axis_ [1].y + axis_ [2].y;
				vertex_ [0].z = center_.z + axis_ [0].z + axis_ [1].z + axis_ [2].z;
				vertex_ [1].x = center_.x + axis_ [0].x + axis_ [1].x - axis_ [2].x;
				vertex_ [1].y = center_.y + axis_ [0].y + axis_ [1].y - axis_ [2].y;
				vertex_ [1].z = center_.z + axis_ [0].z + axis_ [1].z - axis_ [2].z;

				vertex_ [2].x = center_.x + axis_ [0].x - axis_ [1].x + axis_ [2].x;
				vertex_ [2].y = center_.y + axis_ [0].y - axis_ [1].y + axis_ [2].y;
				vertex_ [2].z = center_.z + axis_ [0].z - axis_ [1].z + axis_ [2].z;

				vertex_ [3].x = center_.x + axis_ [0].x - axis_ [1].x - axis_ [2].x;
				vertex_ [3].y = center_.y + axis_ [0].y - axis_ [1].y - axis_ [2].y;
				vertex_ [3].z = center_.z + axis_ [0].z - axis_ [1].z - axis_ [2].z;

				vertex_ [4].x = center_.x - axis_ [0].x + axis_ [1].x + axis_ [2].x;
				vertex_ [4].y = center_.y - axis_ [0].y + axis_ [1].y + axis_ [2].y;
				vertex_ [4].z = center_.z - axis_ [0].z + axis_ [1].z + axis_ [2].z;

				vertex_ [5].x = center_.x - axis_ [0].x + axis_ [1].x - axis_ [2].x;
				vertex_ [5].y = center_.y - axis_ [0].y + axis_ [1].y - axis_ [2].y;
				vertex_ [5].z = center_.z - axis_ [0].z + axis_ [1].z - axis_ [2].z;

				vertex_ [6].x = center_.x - axis_ [0].x - axis_ [1].x + axis_ [2].x;
				vertex_ [6].y = center_.y - axis_ [0].y - axis_ [1].y + axis_ [2].y;
				vertex_ [6].z = center_.z - axis_ [0].z - axis_ [1].z + axis_ [2].z;

				vertex_ [7].x = center_.x - axis_ [0].x - axis_ [1].x - axis_ [2].x;
				vertex_ [7].y = center_.y - axis_ [0].y - axis_ [1].y - axis_ [2].y;
				vertex_ [7].z = center_.z - axis_ [0].z - axis_ [1].z - axis_ [2].z;
			}

			return BoxTest(vertex_, 8);
		}


	}
}
