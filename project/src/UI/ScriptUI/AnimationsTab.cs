using System.Collections.Generic;

namespace Cue
{
	class PersonAnimationsTab : Tab
	{
		private Person person_;
		private VUI.ListView<Animation> anims_ = new VUI.ListView<Animation>();
		private VUI.CheckBox loop_ = new VUI.CheckBox("Loop");
		private VUI.CheckBox paused_ = new VUI.CheckBox("Paused");
		private VUI.FloatTextSlider seek_ = new VUI.FloatTextSlider();
		private IgnoreFlag ignore_ = new IgnoreFlag();
		private Animation sel_ = null;

		public PersonAnimationsTab(Person person)
			: base("Animations")
		{
			person_ = person;

			Layout = new VUI.BorderLayout();

			var top = new VUI.Panel(new VUI.VerticalFlow());

			var p = new VUI.Panel(new VUI.HorizontalFlow());
			p.Add(new VUI.Button("Stand", () => person_.SetState(PersonState.Standing)));
			p.Add(new VUI.Button("Sit", () => person_.SetState(PersonState.Sitting)));
			p.Add(new VUI.Button("Crouch", () => person_.SetState(PersonState.Crouching)));
			p.Add(new VUI.Button("Straddle sit", () => person_.SetState(PersonState.SittingStraddling)));
			top.Add(p);

			p = new VUI.Panel(new VUI.HorizontalFlow());
			p.Add(new VUI.Button("Play", OnPlay));
			p.Add(new VUI.Button("Stop", OnStop));
			p.Add(paused_);
			p.Add(loop_);
			top.Add(p);

			p = new VUI.Panel(new VUI.BorderLayout());
			p.Add(seek_, VUI.BorderLayout.Center);
			top.Add(p);


			Add(top, VUI.BorderLayout.Top);
			Add(anims_, VUI.BorderLayout.Center);

			var items = new List<Animation>();
			foreach (var a in Resources.Animations.GetAll(Animation.NoType, person_.MovementStyle))
				items.Add(a);

			anims_.SetItems(items);

			paused_.Changed += OnPaused;
			seek_.ValueChanged += OnSeek;
		}

		public override void Update(float s)
		{
			if (sel_ == null || person_.Animator.CurrentAnimation != sel_)
				return;

			var p = person_.Animator.CurrentPlayer;

			if (p != null && !p.Paused)
			{
				ignore_.Do(() =>
				{
					seek_.WholeNumbers = p.UsesFrames;
					seek_.Set(
						sel_.Real.FirstFrame, sel_.Real.FirstFrame,
						sel_.Real.LastFrame);
				});
			}
		}

		private void OnPlay()
		{
			sel_ = anims_.Selected;
			if (sel_ == null)
				return;

			PlaySelection();
		}

		private void PlaySelection(float frame = -1)
		{
			person_.Animator.Play(
				sel_, (loop_.Checked ? Animator.Loop : 0) | Animator.Exclusive);

			var p = person_.Animator.CurrentPlayer;
			if (p == null)
			{
				// todo
				return;
			}

			p.Paused = paused_.Checked;

			if (paused_.Checked)
			{
				((BVH.Player)p).ShowSkeleton();
				p.Seek(sel_.Real.InitFrame);
			}

			ignore_.Do(() =>
			{
				seek_.WholeNumbers = p.UsesFrames;

				if (frame < 0)
					frame = sel_.Real.InitFrame;

				seek_.Set(frame, sel_.Real.InitFrame, sel_.Real.LastFrame);
			});
		}

		private void OnStop()
		{
			if (sel_ == null || person_.Animator.CurrentAnimation != sel_)
				return;

			person_.Animator.Stop();
		}

		private void OnPaused(bool b)
		{
			if (sel_ == null || person_.Animator.CurrentAnimation != sel_)
				return;

			var p = person_.Animator.CurrentPlayer;
			if (p != null)
				p.Paused = b;
		}

		private void OnSeek(float f)
		{
			if (ignore_ || sel_ == null)
				return;

			if (person_.Animator.CurrentAnimation != sel_)
				PlaySelection(f);

			if (person_.Animator.CurrentPlayer == null)
			{
				Cue.LogError("no player");
				return;
			}

			Cue.LogInfo($"seeking to {f}");
			person_.Animator.CurrentPlayer.Seek(f);
		}
	}
}
