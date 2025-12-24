using UnityEngine;
using System.Collections;
using Nova;

public class MusicGestureHandler : MonoBehaviour
{
    [Header("NOVA DATA")]
    public UIBlock Next;
    public UIBlock Prev;
    public UIBlock Play;

    private UIBlock2D playBlock;
    private UIBlock2D nextBlock;
    private UIBlock2D prevBlock;

    public MusicSetting[] MusicSetting;
    public ItemView MusicView;

    [Header("INTERNALS")]
    public Sprite playIcon;
    public Sprite pauseIcon;
    public AudioSource audioSource;
    [Space(10)]
    public UIBlock2D durationBar;

    private bool isPlaying = false;
    int musicIndex = 0;

    private Coroutine nextScaleRoutine;
    private Coroutine prevScaleRoutine;
    private Coroutine playScaleRoutine;
    private Coroutine durationRoutine;

    void Start(){
        // NEXT
        Next.AddGestureHandler<Gesture.OnPress>(OnNextClicked);
        Next.AddGestureHandler<Gesture.OnHover>(OnNextHover);
        Next.AddGestureHandler<Gesture.OnUnhover>(OnNextUnhover);

        // PREV
        Prev.AddGestureHandler<Gesture.OnPress>(OnPrevClicked);
        Prev.AddGestureHandler<Gesture.OnHover>(OnPrevHover);
        Prev.AddGestureHandler<Gesture.OnUnhover>(OnPrevUnhover);

        // PLAY
        Play.AddGestureHandler<Gesture.OnPress>(OnPlayClicked);
        Play.AddGestureHandler<Gesture.OnHover>(OnPlayHover);
        Play.AddGestureHandler<Gesture.OnUnhover>(OnPlayUnhover);

        playBlock = Play.GetComponent<UIBlock2D>();
        nextBlock = Next.GetComponent<UIBlock2D>();
        prevBlock = Prev.GetComponent<UIBlock2D>();

        audioSource.loop = true;

        BindIcon(MusicSetting[0], MusicView.Visuals as MusicVisual);
        PrepareAudio(MusicSetting[0]);
    }

    void BindIcon(MusicSetting setting, MusicVisual visual){
        if(visual == null) return;

        visual.nameText.Text = setting.musicName;
        visual.creatorText.Text = setting.musicCreator;
        visual.iconImage.SetImage(setting.musicIcon);
    }

    void PrepareAudio(MusicSetting setting){
        if(setting == null || setting.musicClip == null) return;
        audioSource.clip = setting.musicClip;
    }

    void PlayCurrent(){
        if(audioSource.clip == null) return;
        audioSource.Play();
        isPlaying = true;
        UpdatePlayIcon();

        if(durationRoutine != null) StopCoroutine(durationRoutine);
        durationRoutine = StartCoroutine(AnimateDurationBar());
    }

    void PauseCurrent(){
        audioSource.Pause();
        isPlaying = false;
        UpdatePlayIcon();

        if(durationRoutine != null) StopCoroutine(durationRoutine);
    }

    void UpdatePlayIcon(){
        var visual = MusicView.Visuals as MusicVisual;
        if(visual == null) return;
        visual.play.SetImage(isPlaying ? pauseIcon : playIcon);
    }

    void ChangeMusic(int index){
        if(index < 0 || index >= MusicSetting.Length) return;

        musicIndex = index;

        BindIcon(MusicSetting[musicIndex], MusicView.Visuals as MusicVisual);
        PrepareAudio(MusicSetting[musicIndex]);

        if(isPlaying)
            audioSource.Play();
    }

    IEnumerator ScaleTo(Transform target, float scale){
        Vector3 start = target.localScale;
        Vector3 end = Vector3.one * scale;
        float t = 0f;

        while(t < 1f){
            t += Time.unscaledDeltaTime * 10f;
            target.localScale = Vector3.Lerp(start, end, t);
            yield return null;
        }

        target.localScale = end;
    }

    #region NEXT LOGIC
    void OnNextClicked(Gesture.OnPress evt){
        int next = (musicIndex + 1) % MusicSetting.Length;
        ChangeMusic(next);
    }

    void OnNextHover(Gesture.OnHover evt){
        nextBlock.Gradient.Enabled = true;

        if(nextScaleRoutine != null) StopCoroutine(nextScaleRoutine);
        nextScaleRoutine = StartCoroutine(ScaleTo(Next.transform, 1.3f));
    }

    void OnNextUnhover(Gesture.OnUnhover evt){
        nextBlock.Gradient.Enabled = false;

        if(nextScaleRoutine != null) StopCoroutine(nextScaleRoutine);
        nextScaleRoutine = StartCoroutine(ScaleTo(Next.transform, 1f));
    }
    #endregion

    #region PREV LOGIC
    void OnPrevClicked(Gesture.OnPress evt){
        int prev = (musicIndex - 1 + MusicSetting.Length) % MusicSetting.Length;
        ChangeMusic(prev);
    }

    void OnPrevHover(Gesture.OnHover evt){
        prevBlock.Gradient.Enabled = true;

        if(prevScaleRoutine != null) StopCoroutine(prevScaleRoutine);
        prevScaleRoutine = StartCoroutine(ScaleTo(Prev.transform, 1.3f));
    }

    void OnPrevUnhover(Gesture.OnUnhover evt){
        prevBlock.Gradient.Enabled = false;

        if(prevScaleRoutine != null) StopCoroutine(prevScaleRoutine);
        prevScaleRoutine = StartCoroutine(ScaleTo(Prev.transform, 1f));
    }
    #endregion

    #region PLAY LOGIC
    void OnPlayClicked(Gesture.OnPress evt){
        if(isPlaying) PauseCurrent();
        else PlayCurrent();
    }

    void OnPlayHover(Gesture.OnHover evt){
        playBlock.Gradient.Enabled = true;

        if(playScaleRoutine != null) StopCoroutine(playScaleRoutine);
        playScaleRoutine = StartCoroutine(ScaleTo(Play.transform, 1.3f));
    }

    void OnPlayUnhover(Gesture.OnUnhover evt){
        playBlock.Gradient.Enabled = false;

        if(playScaleRoutine != null) StopCoroutine(playScaleRoutine);
        playScaleRoutine = StartCoroutine(ScaleTo(Play.transform, 1f));
    }
    #endregion

    IEnumerator AnimateDurationBar(){
        durationBar.Size.X.Value = 0f;
        float maxWidth = 800f;
        float clipLength = audioSource.clip.length;
        float elapsed = 0f;

        while(elapsed < clipLength){
            if(!isPlaying) yield return null;
            else {
                elapsed += Time.unscaledDeltaTime;
                durationBar.Size.X.Value = Mathf.Lerp(0f, maxWidth, elapsed / clipLength);
                yield return null;
            }
        }

        durationBar.Size.X.Value = maxWidth;
    }
}
