using System;
using GTA;
using GTA.Native;
using ZombiesMod.Wrappers;

namespace ZombiesMod.Extensions;

public static class PedExtended
{
	internal static readonly string[] SpeechModifierNames = new string[37]
	{
		"SPEECH_PARAMS_STANDARD", "SPEECH_PARAMS_ALLOW_REPEAT", "SPEECH_PARAMS_BEAT", "SPEECH_PARAMS_FORCE", "SPEECH_PARAMS_FORCE_FRONTEND", "SPEECH_PARAMS_FORCE_NO_REPEAT_FRONTEND", "SPEECH_PARAMS_FORCE_NORMAL", "SPEECH_PARAMS_FORCE_NORMAL_CLEAR", "SPEECH_PARAMS_FORCE_NORMAL_CRITICAL", "SPEECH_PARAMS_FORCE_SHOUTED",
		"SPEECH_PARAMS_FORCE_SHOUTED_CLEAR", "SPEECH_PARAMS_FORCE_SHOUTED_CRITICAL", "SPEECH_PARAMS_FORCE_PRELOAD_ONLY", "SPEECH_PARAMS_MEGAPHONE", "SPEECH_PARAMS_HELI", "SPEECH_PARAMS_FORCE_MEGAPHONE", "SPEECH_PARAMS_FORCE_HELI", "SPEECH_PARAMS_INTERRUPT", "SPEECH_PARAMS_INTERRUPT_SHOUTED", "SPEECH_PARAMS_INTERRUPT_SHOUTED_CLEAR",
		"SPEECH_PARAMS_INTERRUPT_SHOUTED_CRITICAL", "SPEECH_PARAMS_INTERRUPT_NO_FORCE", "SPEECH_PARAMS_INTERRUPT_FRONTEND", "SPEECH_PARAMS_INTERRUPT_NO_FORCE_FRONTEND", "SPEECH_PARAMS_ADD_BLIP", "SPEECH_PARAMS_ADD_BLIP_ALLOW_REPEAT", "SPEECH_PARAMS_ADD_BLIP_FORCE", "SPEECH_PARAMS_ADD_BLIP_SHOUTED", "SPEECH_PARAMS_ADD_BLIP_SHOUTED_FORCE", "SPEECH_PARAMS_ADD_BLIP_INTERRUPT",
		"SPEECH_PARAMS_ADD_BLIP_INTERRUPT_FORCE", "SPEECH_PARAMS_FORCE_PRELOAD_ONLY_SHOUTED", "SPEECH_PARAMS_FORCE_PRELOAD_ONLY_SHOUTED_CLEAR", "SPEECH_PARAMS_FORCE_PRELOAD_ONLY_SHOUTED_CRITICAL", "SPEECH_PARAMS_SHOUTED", "SPEECH_PARAMS_SHOUTED_CLEAR", "SPEECH_PARAMS_SHOUTED_CRITICAL"
	};

	public static void PlayPain(this Ped ped, int type)
	{
		Function.Call((Hash)0xBC9AE166038A5CECuL, new InputArgument[4] { ped.Handle, type, 0, 0 });
	}

	public static void PlayFacialAnim(this Ped ped, string animSet, string animName)
	{
		Function.Call((Hash)0xE1E65CA8AC9C00EDuL, new InputArgument[3] { ped.Handle, animName, animSet });
	}

	public static bool HasBeenDamagedByMelee(this Ped ped)
	{
		return Function.Call<bool>((Hash)0x131D401334815E94uL, new InputArgument[3] { ped.Handle, 0, 1 });
	}

	public static bool HasBeenDamagedBy(this Ped ped, WeaponHash weapon)
	{
		return Function.Call<bool>((Hash)0x131D401334815E94uL, new InputArgument[3]
		{
			ped.Handle,
			(int)weapon,
			0
		});
	}

	public unsafe static Bone LastDamagedBone(this Ped ped)
	{
		int result = default(int);
		if (Function.Call<bool>((Hash)0xD75960F6BD9EA49CuL, new InputArgument[2]
		{
			ped.Handle,
			&result
		}))
		{
			return (Bone)result;
		}
		return Bone.SkelRoot;
	}

	public static void SetPathAvoidWater(this Ped ped, bool toggle)
	{
		Function.Call((Hash)0x38FE1EC73743793CuL, new InputArgument[2]
		{
			ped.Handle,
			toggle ? 1 : 0
		});
	}

	public static void SetStealthMovement(this Ped ped, bool toggle)
	{
		// SET_PED_STEALTH_MOVEMENT(ped, toggle, action) — the ped handle was missing,
		// so the toggle was being interpreted as the ped and the call did nothing useful.
		Function.Call((Hash)0x88CBB5CEB96B7BD2uL, ped.Handle, toggle ? 1 : 0, "DEFAULT_ACTION");
	}

	public static bool GetStealthMovement(this Ped ped)
	{
		return Function.Call<bool>((Hash)0x7C2AC9CA66575FBFuL, new InputArgument[1] { ped.Handle });
	}

	public static void SetComponentVariation(this Ped ped, ComponentId id, int drawableId, int textureId, int paletteId)
	{
		Function.Call((Hash)0x262B14F48D29DE80uL, new InputArgument[5]
		{
			ped.Handle,
			(int)id,
			drawableId,
			textureId,
			paletteId
		});
	}

	public static int GetDrawableVariation(this Ped ped, ComponentId id)
	{
		return Function.Call<int>((Hash)0x67F3780DD425D4FCuL, new InputArgument[2]
		{
			ped.Handle,
			(int)id
		});
	}

	public static int GetNumberOfDrawableVariations(this Ped ped, ComponentId id)
	{
		return Function.Call<int>((Hash)0x27561561732A7842uL, new InputArgument[2]
		{
			ped.Handle,
			(int)id
		});
	}

	public static bool IsSubttaskActive(this Ped ped, Subtask task)
	{
		return Function.Call<bool>((Hash)0xB0760331C7AA4155uL, new InputArgument[2]
		{
			ped,
			(int)task
		});
	}

	public static bool IsDriving(this Ped ped)
	{
		return ped.IsSubttaskActive(Subtask.DrivingWandering) || ped.IsSubttaskActive(Subtask.DrivingGoingToDestinationOrEscorting);
	}

	public static void SetPathCanUseLadders(this Ped ped, bool toggle)
	{
		Function.Call((Hash)0x77A5B103C87F476EuL, new InputArgument[2]
		{
			ped.Handle,
			toggle ? 1 : 0
		});
	}

	public static void SetPathCanClimb(this Ped ped, bool toggle)
	{
		Function.Call((Hash)0x8E06A6FE76C9EFF4uL, new InputArgument[2]
		{
			ped.Handle,
			toggle ? 1 : 0
		});
	}

	public static void SetMovementAnimSet(this Ped ped, string animation)
	{
		if (!(ped == null))
		{
			while (!Function.Call<bool>((Hash)0xC4EA073D86FB29B0uL, new InputArgument[1] { animation }))
			{
				Function.Call((Hash)0x6EA47DAE7FAD0EEDuL, new InputArgument[1] { animation });
				Script.Yield();
			}
			Function.Call((Hash)0xAF8A94EDE7712BEFuL, new InputArgument[3] { ped.Handle, animation, 1048576000 });
		}
	}

	public static void RemoveElegantly(this Ped ped)
	{
		Function.Call((Hash)0xAC6D445B994DF95EuL, new InputArgument[1] { ped.Handle });
	}

	public static void SetRagdollOnCollision(this Ped ped, bool toggle)
	{
		Function.Call((Hash)0xF0A4F1BBF4FA7497uL, new InputArgument[2] { ped.Handle, toggle });
	}

	public static void SetAlertness(this Ped ped, Alertness alertness)
	{
		Function.Call((Hash)0xDBA71115ED9941A6uL, new InputArgument[2]
		{
			ped.Handle,
			(int)alertness
		});
	}

	public static void SetCombatAblility(this Ped ped, CombatAbility ability)
	{
		Function.Call((Hash)0xC7622C0D36B2FDA8uL, new InputArgument[2]
		{
			ped.Handle,
			(int)ability
		});
	}

	public static void SetCanEvasiveDive(this Ped ped, bool toggle)
	{
		Function.Call((Hash)0x6B7A646C242A7059uL, new InputArgument[2]
		{
			ped.Handle,
			toggle ? 1 : 0
		});
	}

	public static void StopAmbientSpeechThisFrame(this Ped ped)
	{
		if (ped.IsAmbientSpeechPlaying())
		{
			Function.Call((Hash)0xB8BEC0CA6F0EDB0FuL, new InputArgument[1] { ped.Handle });
		}
	}

	public static bool IsAmbientSpeechPlaying(this Ped ped)
	{
		return Function.Call<bool>((Hash)0x9072C8B49907BFADuL, new InputArgument[1] { ped.Handle });
	}

	public static void DisablePainAudio(this Ped ped, bool toggle)
	{
		Function.Call((Hash)0xA9A41C1E940FB0E8uL, new InputArgument[2]
		{
			ped.Handle,
			toggle ? 1 : 0
		});
	}

	public static void StopSpeaking(this Ped ped, bool shaking)
	{
		Function.Call((Hash)0x9D64D7405520E3D3uL, new InputArgument[2]
		{
			ped.Handle,
			shaking ? 1 : 0
		});
	}

	public static void SetCanPlayAmbientAnims(this Ped ped, bool toggle)
	{
		Function.Call((Hash)0x6373D1349925A70EuL, new InputArgument[2]
		{
			ped.Handle,
			toggle ? 1 : 0
		});
	}

	public static void SetCombatAttributes(this Ped ped, CombatAttributes attribute, bool enabled)
	{
		Function.Call((Hash)0x9F7794730795E019uL, new InputArgument[3]
		{
			ped.Handle,
			(int)attribute,
			enabled
		});
	}

	public static void SetPathAvoidFires(this Ped ped, bool toggle)
	{
		Function.Call((Hash)0x4455517B28441E60uL, new InputArgument[2]
		{
			ped.Handle,
			toggle ? 1 : 0
		});
	}

	public static void ApplyDamagePack(this Ped ped, float damage, float multiplier, DamagePack damagePack)
	{
		Function.Call((Hash)0x46DF918788CB093FuL, new InputArgument[4]
		{
			ped.Handle,
			damagePack.ToString(),
			damage,
			multiplier
		});
	}

	public static void SetCanAttackFriendlies(this Ped ped, FirendlyFireType type)
	{
		switch (type)
		{
		case FirendlyFireType.CanAttack:
			Function.Call((Hash)0xB3B1CB349FF9C75DuL, new InputArgument[3] { ped.Handle, true, false });
			break;
		case FirendlyFireType.CantAttack:
			Function.Call((Hash)0xB3B1CB349FF9C75DuL, new InputArgument[3] { ped.Handle, false, false });
			break;
		}
	}

	public static void PlayAmbientSpeech(this Ped ped, string speechName, SpeechModifier modifier = SpeechModifier.Standard)
	{
		if (modifier >= SpeechModifier.Standard && (int)modifier < SpeechModifierNames.Length)
		{
			Function.Call((Hash)0x8E04FEDD28D42462uL, new InputArgument[3]
			{
				ped.Handle,
				speechName,
				SpeechModifierNames[(int)modifier]
			});
			return;
		}
		throw new ArgumentOutOfRangeException("modifier");
	}

	public static void Recruit(this Ped ped, Ped leader, bool canBeTargeted, bool invincible, int accuracy)
	{
		if (!(leader == null))
		{
			ped.LeaveGroup();
			ped.SetRagdollOnCollision(toggle: false);
			ped.Task.ClearAll();
			PedGroup currentPedGroup = leader.PedGroup;
			currentPedGroup.SeparationRange = 2.1474836E+09f;
			if (!currentPedGroup.Contains(leader))
			{
				currentPedGroup.Add(leader, leader: true);
			}
			if (!currentPedGroup.Contains(ped))
			{
				currentPedGroup.Add(ped, leader: false);
			}
			ped.CanBeTargetted = canBeTargeted;
			ped.Accuracy = accuracy;
			ped.IsInvincible = invincible;
			ped.IsPersistent = true;
			ped.RelationshipGroup = leader.RelationshipGroup;
			ped.NeverLeavesGroup = true;
			ped.AttachedBlip?.Delete();
			Blip blip = ped.AddBlip();
			blip.Color = BlipColor.Blue;
			blip.Scale = 0.7f;
			blip.Name = "Friend";
			EntityEventWrapper wrapper = new EntityEventWrapper(ped);
			wrapper.Died += delegate(EntityEventWrapper sender, Entity entity)
			{
				entity.AttachedBlip?.Delete();
				wrapper.Dispose();
			};
			ped.PlayAmbientSpeech("GENERIC_HI");
		}
	}

	public static void Recruit(this Ped ped, Ped leader, bool canBeTargetted)
	{
		ped.Recruit(leader, canBeTargetted, invincible: false, 100);
	}

	public static void Recruit(this Ped ped, Ped leader)
	{
		ped.Recruit(leader, canBeTargetted: true);
	}

	public static void SetCombatRange(this Ped ped, CombatRange range)
	{
		Function.Call((Hash)0x3C606747B23E497BuL, new InputArgument[2]
		{
			ped.Handle,
			(int)range
		});
	}

	public static void SetCombatMovement(this Ped ped, CombatMovement movement)
	{
		Function.Call((Hash)0x4D9CA1009AFBD057uL, new InputArgument[2]
		{
			ped.Handle,
			(int)movement
		});
	}

	public static void ClearFleeAttributes(this Ped ped)
	{
		Function.Call((Hash)0x70A2D1137C8ED7C9uL, new InputArgument[3] { ped.Handle, 0, 0 });
	}

	public static bool IsUsingAnyScenario(this Ped ped)
	{
		return Function.Call<bool>((Hash)0x57AB4A3080F85143uL, new InputArgument[1] { ped.Handle });
	}

	public static bool CanHearPlayer(this Ped ped, Player player)
	{
		return Function.Call<bool>((Hash)0xF297383AA91DCA29uL, new InputArgument[2] { player.Handle, ped.Handle });
	}

	public static void SetHearingRange(this Ped ped, float hearingRange)
	{
		Function.Call((Hash)0x33A8F7F7D5F7F33CuL, new InputArgument[2] { ped.Handle, hearingRange });
	}

	public static bool IsCurrentWeaponSileced(this Ped ped)
	{
		return Function.Call<bool>((Hash)0x65F0C5AE05943EC7uL, new InputArgument[1] { ped.Handle });
	}

	public static void Jump(this Ped ped)
	{
		Function.Call((Hash)0x0AE4086104E067B1uL, new InputArgument[4] { ped.Handle, true, 0, 0 });
	}

	public static void SetToRagdoll(this Ped ped, int time)
	{
		Function.Call((Hash)0xAE99FB955581844AuL, new InputArgument[7] { ped.Handle, time, 0, 0, 0, 0, 0 });
	}
}
