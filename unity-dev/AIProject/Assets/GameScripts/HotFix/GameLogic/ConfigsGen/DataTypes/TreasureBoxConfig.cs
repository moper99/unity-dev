using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable InconsistentNaming
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable PartialTypeWithSinglePart
// 对应的Excel表格 B宝箱表.xlsx[Sheet1]
#nullable enable
namespace GameLogic {
	public partial class TreasureBoxConfig
	{
		public readonly int id; // 序号
		public readonly I18NString name; // 名称
		public readonly int boxType; // 宝箱类型（1：钓鱼宝箱 2：出售鱼宝箱 3:新手引导用的）
		public readonly I18NString des; // 详情描述
		public readonly int type; // 宝箱类型(1给全部奖励2权重给其中一个）
		public readonly IReadOnlyList<Tuple<int,int,int>> boxItem; // 宝箱内容
		public readonly IReadOnlyList<int> weight; // 物品权重概率
		public readonly string iconAsset; // 资源名
		public readonly string assetName; // 宝箱资源名
		public readonly int quality; // 宝箱品质
		public readonly int boxWeighting; // 出现权重
		public readonly Vector3 pos; // 位置
		public readonly Vector3 rotation; // 角度
		public readonly Vector3 scale; // 缩放
		public readonly int modelScaleId; // 鱼影ID
		public readonly float bite; // 上钩步长 每次点击百分比
		public readonly float struggle; // 挣扎回退速度
		public readonly float defaultSescale; // 默认鱼缩放

		public TreasureBoxConfig(int id,
			I18NString name,
			int boxType,
			I18NString des,
			int type,
			IReadOnlyList<Tuple<int,int,int>> boxItem,
			IReadOnlyList<int> weight,
			string iconAsset,
			string assetName,
			int quality,
			int boxWeighting,
			Vector3 pos,
			Vector3 rotation,
			Vector3 scale,
			int modelScaleId,
			float bite,
			float struggle,
			float defaultSescale)
		{
			this.id = id;
			this.name = name;
			this.boxType = boxType;
			this.des = des;
			this.type = type;
			this.boxItem = boxItem;
			this.weight = weight;
			this.iconAsset = iconAsset;
			this.assetName = assetName;
			this.quality = quality;
			this.boxWeighting = boxWeighting;
			this.pos = pos;
			this.rotation = rotation;
			this.scale = scale;
			this.modelScaleId = modelScaleId;
			this.bite = bite;
			this.struggle = struggle;
			this.defaultSescale = defaultSescale;
		}
		public override string ToString()
		{
			return $"TreasureBoxConfig(id={id},name={name},boxType={boxType},des={des},type={type},boxItem={boxItem},weight={weight},iconAsset={iconAsset},assetName={assetName},quality={quality},boxWeighting={boxWeighting},pos={pos},rotation={rotation},scale={scale},modelScaleId={modelScaleId},bite={bite},struggle={struggle},defaultSescale={defaultSescale})";
		}
	}
}
