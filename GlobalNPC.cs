using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SegmentedEnemyChanges {
	public class GNPC : GlobalNPC {
		public override bool InstancePerEntity { get { return true; } }
		bool setStats = false;

		private bool IsSegmented(NPC npc) {
			return npc.realLife >= 0 && Main.npc[npc.realLife].active;
		}

		public override bool PreAI(NPC npc) {
			if (IsSegmented(npc)) {
				if (!setStats) {
					npc.life /= 2;
					npc.lifeMax /= 2;
					if (npc.whoAmI != npc.realLife) {
						NPC parent = Main.npc[npc.realLife]; // Just in case some mod sets different stats on segments, when really they should all be the same.
						npc.damage = parent.damage;
						npc.defDamage = parent.defDamage;
						npc.defDefense = parent.defDefense;
						npc.defense = parent.defense;
						npc.life = parent.life;
						npc.lifeMax = parent.lifeMax;
					}
					setStats = true;
				}
			}
			return base.PreAI(npc);
		}

		public override void OnHitByItem (NPC npc, Player player, Item item, int damage, float knockback, bool crit) {
			npc.immune[player.whoAmI] = player.itemAnimation;
			MakeSegmentsImmune(npc, player);
		}

		public override bool? CanBeHitByProjectile (NPC npc, Projectile projectile) {
			if (projectile.usesLocalNPCImmunity && projectile.localNPCImmunity[npc.whoAmI] > 0) {
				return false;
			}
			return null;
		}

		public override void OnHitByProjectile (NPC npc, Projectile projectile, int damage, float knockback, bool crit) {
			if (IsSegmented(npc)) {
				projectile.usesLocalNPCImmunity = true;
				// TODO: Make immunity depend on how far the projectile travels rather than a fixed time. Really fast projectiles are screwed here. Also reset immunity if projectile doesn't collide at all.
				projectile.localNPCHitCooldown = (projectile.localNPCHitCooldown > 0 ? projectile.localNPCHitCooldown : (NPC.immuneTime / 2)) * ((int)Math.Sqrt(projectile.extraUpdates) + 1);
				projectile.localNPCImmunity[npc.whoAmI] = projectile.localNPCHitCooldown;
				MakeSegmentsImmune(npc, Main.player[projectile.owner], projectile);
			}
		}

		public override void UpdateLifeRegen (NPC npc, ref int damage) {
			if (IsSegmented(npc) && npc.whoAmI != npc.realLife) { // Stops segments other than the head from taking regen damage or giving regen life. Prevents cheesing damage with debuffs like fire.
				damage = 0;
				npc.lifeRegen = 0;
			}
		}

		private void MakeSegmentsImmune(NPC npc, Player player, Projectile projectile = null) { // Makes all segments of a segmented NPC immune when hit to avoid insane damages
			if (IsSegmented(npc)) {
				bool last = false;
				NPC parent = Main.npc[npc.realLife];
				parent.lifeRegen = npc.lifeRegen; // Make the head use the life regen that segment uses
				for (int i = 0; ((parent.ai[0] > 0 && Main.npc[(int)parent.ai[0]].active) || last) && i < 200; i++) { // As long as it respects using ai[0] for segments
					if (projectile != null) {
						projectile.localNPCImmunity[parent.whoAmI] = projectile.localNPCImmunity[npc.whoAmI]; // Share projectile immunity
					}
					else {
						parent.immune[player.whoAmI] = npc.immune[player.whoAmI]; // Share melee immunity
					}
					for (int j=0; j < npc.buffType.Length; j++) { // Share the debuffs between all segments
						if (npc.buffType[j] > 0 && npc.buffTime[j] > 0) {
							parent.buffType[j] = npc.buffType[j];
							parent.buffTime[j] = npc.buffTime[j];
						}
					}
					if (last) { break; }
					parent = Main.npc[(int)parent.ai[0]];
					if (parent.ai[0] == 0) { last = true; } // If it's the tail tip
				}
			}
		}
	}
}
