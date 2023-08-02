using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Project.Lib {
	//遮蔽板
	/*class OcclusionObject
	{
		//QuadData	quad;
		float		radius;
		//頂点
		FVector3	vertex[QUAD_VERTEX_MAX];
		//カメラに一番近い頂点のz座標
		float		nearZ;		
		//前面のデータ
		PlaneData	front;
		//上下左右の4面の法線ベクトル(原点を通ることがわかっているのでdは常に0)
		FVector3	normal[OCCLUSION_SIDE_PLANE_MAX];
	};*/

	/// <summary>
	/// 遮蔽カリング
	/// </summary>
	public class OcclusionCulling : FrustumCulling{

		List<Occluder> occluderList_ = new List<Occluder>();
		/// <summary>
		/// 遮蔽オブジェクトの視錐台カリング
		/// </summary>
		public bool CullingTest(Occluder occluder)
		{
			occluder.CalcViewMatrix (matrix_);

			//カリングokの場合は遮蔽カリングのための準備を行わない
			if (BoxTest (occluder.cameraPoint, Occluder.OCCLUDER_POINT))
				return true;

			occluder.CalcNearZ ();
			occluder.calcOcclusionPlane ();
			occluderList_.Add (occluder);

			return false;
		}
		/// <summary>
		/// 遮蔽オブジェクトをz値でソート
		/// </summary>
		public void SortOccluderList(){
			//@note 状況に合わせてソート方法は適当にかえる
			//不要なメモリ確保しないのを優先して。バブルソート
			for (int i = 0, max = occluderList_.Count; i < max; i++) {
				for (int j = 0; j < max; j++) {
					if (occluderList_ [i].nearZ < occluderList_ [j].nearZ) {
						break;
					}

					Occluder tmp = occluderList_ [i];
					occluderList_ [i] = occluderList_ [j];
					occluderList_ [j] = tmp;
				}
			}

		}

		/// <summary>
		/// ビュー行列の更新とリストの初期化
		/// </summary>
		public override void Execute(Matrix4x4 matrix) {
			//ビュー行列の設定
			matrix_ = matrix;
			//検査対象のリストを初期化
			occluderList_.Clear();
		}

		/// <summary>
		/// 遮蔽オブジェクトの遮蔽カリング
		/// </summary>
		//bool preOcclusionCulling()



		/// <summary>
		/// 遮蔽空間に含まれているかテスト
		/// </summary>
		bool OcclusionTest(Occluder occluder, Vector3[] vertex, int max)
		{
			//全部の頂点が遮蔽空間の中にあるかテスト
			for (int i = 0; i < max; i++) {
				//点と平面の距離計算のうち、0になるのが判明している部分を予め除いて計算する
				//上下左右面の検査
				for (int j = 0; j < Occluder.OCCLUDER_POINT; j++) {
					if ((occluder.normal [j].x * vertex[i].x + occluder.normal [j].y * vertex[i].y + occluder.normal [j].z * vertex[i].z) < 0) {
						return false;
					}
				}

				//前面の検査
				if ((occluder.front.normal.x * vertex[i].x + occluder.front.normal.y * vertex[i].y + occluder.front.normal.z * vertex[i].z) < -occluder.front.d) {
					return false;
				}
			}
			return true;
		}


		/// <summary>
		/// 視錐台+遮蔽カリング(境界BOX)
		/// </summary>
		public override bool CullingTest(ShapeCollision.OBB obb, float circumscribedRadius) {
			return CullingTest(obb.center, obb.axis, obb.radius, circumscribedRadius);
		}
		/// <summary>
		/// 視錐台+遮蔽カリング(境界BOX)
		/// </summary>
		public override bool CullingTest(Vector3 center, Vector3[] axis, float[] radius, float circumscribedRadius) {
			int index;
			return CullingTest(center, axis, radius, circumscribedRadius, out index);
		}
		/// <summary>
		/// 視錐台+遮蔽カリング(境界BOX)
		/// </summary>
		public bool CullingTest(Vector3 center, Vector3[] axis, float[] radius, float circumscribedRadius, out int index) {
			index = -1;
			//まずは基底の視錐台カリングをテストする
			if (base.CullingTest(center, axis, radius, circumscribedRadius))
				return true;

			//視錐台の中に含まれている場合は遮蔽カリングをテスト
			for (int i = 0, max = occluderList_.Count; i < max; i++) {
				//視錐台カリングのテストで使ったテンポラリを流用して無駄な行列変換を省く
				if (OcclusionTest(occluderList_[i], vertex_, 8)) {
					index = i;
					return true;
				}
			}

			//どのテストも合格しなかったらカリングできないと判断
			return false;
		}

	}
}

/*
 * namespace
{
//遮蔽オブジェクトリスト
StackLite<OcclusionObject,  8> list_;
StackLite<OcclusionObject*, 8> preList_;
StackLite<OcclusionObject*, 8> mainList_;
}

/// <summary>
/// 遮蔽オブジェクトリストをクリア
/// </summary>
void OcclusionCulling::initOcclusionList()
{
	list_.init();
	preList_.init();
	mainList_.init();
}

/// <summary>
/// 遮蔽オブジェクトをリストに追加
/// </summary>
void OcclusionCulling::addOcclusionList(const QuadData& data)
{
	OcclusionObject obj;
	obj.quad = data;
	obj.radius = sqrt(data.radius[0] * data.radius[0] + data.radius[1] * data.radius[1]);
	list_.push(obj);
}

/// <summary>
/// 遮蔽オブジェクトのカリングテスト
/// </summary>
void OcclusionCulling::prepOcclusionList(const Matrix44& matrix)
{
	preList_.init();
	for(int i = 0; i < list_.getCount(); i++)
	{
		bool result = preFrustumCulling(list_[i], matrix);
		//視錐台の中に含まれている場合は遮蔽検査リストに追加
		if(result == false)
		{
			calcNearZ(list_[i]);
			preList_.push(&list_[i]);
		}
	}

	int max = preList_.getCount();
	mainList_.init();

	for(int loop = 0; loop < max; loop++)
	{
		//リストの中で一番カメラに近いものを探す
		int nearest = calcNearest();
		//検査していないオブジェクトがなくなったら検査終了
		if(nearest < 0)
		{
			break;
		}

		//一番近い遮蔽オブジェクトは本番用のリストに移動して検査リストからはずす
		OcclusionObject* obj = preList_[nearest];
		mainList_.push(preList_[nearest]);
		preList_[nearest] = NULL;
		//遮蔽平面を計算
		calcOcclusionPlane(obj);


		//遮蔽の検査実施して遮蔽されていたらリストからはずす
		for(int i = 0; i < preList_.getCount(); i++)
		{
			if(preList_[i] == NULL)
			{
				continue;
			}
			bool result = preOcclusionCulling(obj, preList_[i]);
			//遮蔽されている場合はリストから削除
			if(result == true)
			{
				preList_[i] = NULL;
			}
		}
	}
}


/// <summary>
/// 遮蔽オブジェクトの視錐台カリング
/// </summary>
bool OcclusionCulling::preFrustumCulling(OcclusionObject& obj, const Matrix44& matrix)
{
	//プレ計算としてBOXの外接球で計算する
	SphereData sphereData;

	QuadData& src = obj.quad;
	//中心を座標変換
	sphereData.center = matrix * src.center;
	sphereData.radius = obj.radius;

	bool result = FrustumCulling::preSphereTest(sphereData);
	//カリングOKの場合はここで終了
	if(result)
	{
		return true;
	}

	//四辺形の4頂点が全て視錐台の外にあるかチェック
	FVector3 axis[2];
	axis[0] = matrix.rotation(src.axis[0]) * src.radius[0];
	axis[1] = matrix.rotation(src.axis[1]) * src.radius[1];


	obj.vertex[0] = sphereData.center + axis[0] + axis[1];
	obj.vertex[1] = sphereData.center + axis[0] - axis[1];
	obj.vertex[2] = sphereData.center - axis[0] - axis[1];
	obj.vertex[3] = sphereData.center - axis[0] + axis[1];

	result = FrustumCulling::mainBoxTest(obj.vertex, QUAD_VERTEX_MAX);

	return result;

}

/// <summary>
/// 遮蔽オブジェクトの遮蔽カリング
/// </summary>
bool OcclusionCulling::preOcclusionCulling(OcclusionObject* tester, OcclusionObject* target)
{
	for(int i = 0; i < QUAD_VERTEX_MAX; i++)
	{
		//遮蔽されていない点が見つかった
		if(occlusionTest(tester, target->vertex[i]) == false)
		{
			return false;
		}
	}

	return true;
}


/// <summary>
/// 遮蔽平面面を計算
/// </summary>
void OcclusionCulling::calcOcclusionPlane(OcclusionObject* obj)
{
	//まずは前面を計算
	obj->front = CollisionUtility::calcPlaneFromTriangle(obj->vertex[0], obj->vertex[1], obj->vertex[2]);
	//dの値から遮蔽オブジェクトが表か裏かを調べる
	//原点と平面の距離=-dなので、dがプラスの場合は裏、マイナスの場合は表
	//表の場合
	if(obj->front.d > 0)
	{
		obj->normal[0] = cross(obj->vertex[0], obj->vertex[1]);
		obj->normal[1] = cross(obj->vertex[1], obj->vertex[2]);
		obj->normal[2] = cross(obj->vertex[2], obj->vertex[3]);
		obj->normal[3] = cross(obj->vertex[3], obj->vertex[0]);
	}
	//裏の場合
	else
	{
		obj->normal[0] = cross(obj->vertex[1], obj->vertex[0]);
		obj->normal[1] = cross(obj->vertex[2], obj->vertex[1]);
		obj->normal[2] = cross(obj->vertex[3], obj->vertex[2]);
		obj->normal[3] = cross(obj->vertex[0], obj->vertex[3]);

		//法線ベクトルをを反転
		obj->front.normal.x = -obj->front.normal.x;
		obj->front.normal.y = -obj->front.normal.y;
		obj->front.normal.z = -obj->front.normal.z;
	}
	//法線を正規化
	for(int i = 0; i < OCCLUSION_SIDE_PLANE_MAX; i++)
	{
		obj->normal[i].normalize();
	}
	obj->front.normal.normalize();

}

/// <summary>
/// カメラに一番近い遮蔽オブジェクトを探す
/// </summary>
int OcclusionCulling::calcNearest()
{
	//リストの中で一番カメラに近いものを探す
	int nearest = -1;
	for(int i = 0; i < preList_.getCount(); i++)
	{
		if(preList_[i] == NULL)
		{
			continue;
		}
		//最初のオブジェクトは無条件で入れる
		if(nearest < 0)
		{
			nearest = i;
		}
		else
		{
			//カメラに近い場合は入れ替える
			if( preList_[nearest]->nearZ < preList_[i]->nearZ)
			{
				nearest = i;
			}
		}
	}

	return nearest;
}
/// <summary>
/// 一番手前の頂点のz座標を計算
/// </summary>
void OcclusionCulling::calcNearZ(OcclusionObject& obj)
{
	//z座標がカメラに近いものから順に検査するので
	//一番カメラに近い頂点を求めてz座標を保管する
	obj.nearZ = obj.vertex[0].z;
	for(int i = 1; i < QUAD_VERTEX_MAX; i++)
	{
		if(obj.nearZ < obj.vertex[i].z)
		{
			obj.nearZ = obj.vertex[i].z;
		}
	}
}


/// <summary>
/// 点が遮蔽空間に含まれているかテスト
/// </summary>
bool OcclusionCulling::occlusionTest(const OcclusionObject* obj, const FVector3& p)
{
	//点と平面の距離計算のうち、0になるのが判明している部分を予め除いて計算する
	//上下左右面の検査
	for(int i = 0; i < OCCLUSION_SIDE_PLANE_MAX; i++)
	{
		if(inner(obj->normal[i], p) < 0)
		{
			return false;
		}
	}

	//前面の検査
	const PlaneData& plane = obj->front;
	if(inner(plane.normal, p) < plane.d)
	{
		return false;
	}

	return true;
}




/// <summary>
/// 境界BOXのカリング
/// </summary>
bool OcclusionCulling::cullingTest(const OBBData& src, float radius, const Matrix44& matrix)
{
	//プレ計算としてBOXの外接球で計算する
	SphereData sphereData;
	//中心を座標変換
	sphereData.center = matrix * src.center;
	sphereData.radius = radius;

	bool result = preSphereTest(sphereData);
	//カリングOKの場合はここで終了
	if(result)
	{
		return true;
	}
	//遮蔽カリングしない場合は内接球が視錐台に含まれるかチェックして
	//含まれる場合はボックステストをせずに描画すると処理が少し軽くなる
	//逆に遮蔽カリングする場合は視錐台の中に入っていても遮蔽されている可能性があるので
	//ここではまだ描画確定できない



	//OBBの8頂点が全て視錐台の外にあるかチェック
	//中心座標は球と同じものが使えるので計算不要
	//各軸を回転のみ行って半径倍する = OBBの各軸半径ベクトル
	FVector3 axis[3];
	axis[0] = matrix.rotation(src.axis[0]) * src.radius[0];
	axis[1] = matrix.rotation(src.axis[1]) * src.radius[1];
	axis[2] = matrix.rotation(src.axis[2]) * src.radius[2];



	FVector3 vertex[8];
	vertex[0] = sphereData.center + axis[0] + axis[1] + axis[2];
	vertex[1] = sphereData.center + axis[0] + axis[1] - axis[2];
	vertex[2] = sphereData.center + axis[0] - axis[1] + axis[2];
	vertex[3] = sphereData.center + axis[0] - axis[1] - axis[2];
	vertex[4] = sphereData.center - axis[0] + axis[1] + axis[2];
	vertex[5] = sphereData.center - axis[0] + axis[1] - axis[2];
	vertex[6] = sphereData.center - axis[0] - axis[1] + axis[2];
	vertex[7] = sphereData.center - axis[0] - axis[1] - axis[2];

	result = mainBoxTest(vertex, BOX_VERTEX_MAX);
	//カリングOKの場合はここで終了
	if(result)
	{
		return true;
	}


	//さらに遮蔽カリングテストを行う
	for(int i = 0; i < mainList_.getCount(); i++)
	{
		for(int j = 0; j < BOX_VERTEX_MAX; j++)
		{
			//遮蔽されていない点が見つかったら検査終了
			result = occlusionTest(mainList_[i], vertex[j]);
			if(result == false)
				break;
		}

		//完全に遮蔽しているオブジェクトが一つでも見つかったら描画不要
		if(result == true)
			break;
	}
	//遮蔽カリングテストの結果を最終結果として返す
	return result;
}
*/
