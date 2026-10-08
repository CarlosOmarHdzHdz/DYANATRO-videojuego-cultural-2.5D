using System.Collections.Generic;
using UnityEngine;

// Protagonista 2D: ocho orientaciones y acciones sin arma dibujada.
// El arma equipada pertenece a la recompensa de misión, no a la textura.
[DisallowMultipleComponent]
[DefaultExecutionOrder(150)]
public sealed class XunjuuCompleteSpriteAnimator : MonoBehaviour
{
    [SerializeField] private string completeSheetPath = "Sprites/Player/MacuahuitlComplete_4x4";
    [SerializeField] private string walkSheetPath = "Sprites/Player/MacuahuitlWalk_8x2";
    [SerializeField, Range(80f, 240f)] private float pixelsPerUnit = 170f;
    [SerializeField, Range(3f, 12f)] private float idleFramesPerSecond = 6f;
    [SerializeField, Range(6f, 18f)] private float walkFramesPerSecond = 11f;
    [SerializeField] private bool requireEquippedWeapon;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private PlayerController playerController;
    private Sprite[] completeFrames;
    private Sprite[] walkFrames;
    private Texture2D completeTexture;
    private Texture2D walkTexture;
    private float idleClock;
    private float walkClock;
    private Rigidbody body;
    private int previousRow = -1;
    private float actionClock;
    private Sprite[] refinedFrames;
    private Material refinedMaterial;
    private Material originalMaterial;
    private Sprite[] directionalFrames;
    private Sprite[] directionalActions;
    private static readonly int[] DirectionRows={0,7,2,5,4,5,6,7};
    private Material directionalMaterial;
    private Vector3 previousWorldPosition;
    private float travelled;
    private float walkCycleClock;
    private int activeFrame;
    private Sprite[] balancedWalk;
    private readonly Dictionary<Sprite, HandPose> handPoses = new Dictionary<Sprite, HandPose>();
    private struct HandPose
    {
        public Vector2 localGrip;
        public Sprite foreground;
    }
    private static readonly int[] BalancedRows={0,7,2,5,4,5,2,7};
    private bool balancedActive;
    private float lastMotionTime=-10;
    public bool HasBalancedWalk=>balancedWalk!=null;
    public bool HasSideWalkCorrection {get;private set;}
    private readonly Vector2[] walkRoots=new Vector2[32];
    private readonly Vector2[] actionRoots=new Vector2[32];
    public int ActiveFrame => activeFrame;
    public float TravelledDistance => travelled;
    // Clockwise from screen south: S, SW, W, NW, N, NE, E, SE.
    public int FacingIndex { get; private set; }
    public bool HasDirectionalArt => directionalFrames != null;
    public bool HasRefreshedArt => HasDirectionalArt || refinedFrames != null;
    public bool FacingAway => FacingIndex >= 3 && FacingIndex <= 5;
    public float FacingSign => FacingIndex >= 1 && FacingIndex <= 3 ? -1f : 1f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerController = GetComponent<PlayerController>();
        body = GetComponent<Rigidbody>();
        if(body!=null)body.interpolation=RigidbodyInterpolation.Interpolate;
        previousWorldPosition=transform.position;
        if(animator!=null)animator.applyRootMotion=false;
        completeTexture = Resources.Load<Texture2D>(completeSheetPath);
        walkTexture = Resources.Load<Texture2D>(walkSheetPath);
        completeFrames = BuildGrid(completeTexture, 4, 4, "MacuahuitlComplete");
        walkFrames = BuildGrid(walkTexture, 8, 2, "MacuahuitlWalk");
        BuildRefinedAtlas();
        BuildDirectionalAtlas();
        if (HasDirectionalArt)
        {
            Sprite[] walk = directionalFrames;
            BuildDirectionalAtlas(true);
            directionalActions = directionalFrames;
            directionalFrames = walk;
        }
        BuildBalancedWalk();
        BuildSideWalkCorrection();
        BuildHandPoses();
    }

    private void LateUpdate()
    {
        if (HasDirectionalArt && playerController != null && spriteRenderer != null)
        { AnimateDirectional(); return; }
        if (refinedFrames != null && playerController != null && spriteRenderer != null)
        { AnimateRefined(); return; }
        if (animator == null || spriteRenderer == null || completeFrames == null || completeFrames.Length < 16)
            return;

        if (requireEquippedWeapon && !HasEquippedWeapon())
            return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

        bool jumping = playerController != null && playerController.IsJumping();
        bool attacking = playerController != null && playerController.IsAttacking();
        bool dead = playerController != null && playerController.IsDead();
        Vector3 velocity = body != null ? body.linearVelocity : Vector3.zero;
        bool moving = new Vector2(velocity.x, velocity.z).sqrMagnitude > 0.0025f;
        if (!jumping && !attacking && !dead && moving && walkFrames != null && walkFrames.Length > 0)
        {
            walkClock += Time.deltaTime;
            int walkFrame = Mathf.FloorToInt(walkClock * walkFramesPerSecond) % walkFrames.Length;
            spriteRenderer.sprite = walkFrames[walkFrame];
            idleClock = 0f;
            previousRow = -1;
            return;
        }

        walkClock = 0f;

        int row = 0;
        if (dead)
            row = 3;
        else if (jumping)
            row = 1;
        else if (attacking)
            row = 2;

        if (row != previousRow) actionClock = 0f;
        previousRow = row;
        actionClock += Time.deltaTime;

        int frame;
        if (row == 1 || row == 2)
        {
            frame = row == 1 ? (velocity.y > 0.2f ? 1 : 2)
                : Mathf.Clamp(Mathf.FloorToInt(actionClock / 0.12f), 0, 3);
        }
        else
        {
            idleClock += Time.deltaTime;
            frame = dead ? 1 : 3;
        }

        spriteRenderer.sprite = completeFrames[row * 4 + frame];
    }

    private void OnDestroy()
    {
        foreach (HandPose pose in handPoses.Values)
            if (pose.foreground != null) Destroy(pose.foreground);
        handPoses.Clear();
        if(balancedWalk!=null)foreach(var sprite in balancedWalk)if(sprite!=null)Destroy(sprite);
        if (directionalFrames != null) foreach (Sprite sprite in directionalFrames) if (sprite != null) Destroy(sprite);
        if (directionalActions != null) foreach (Sprite sprite in directionalActions) if (sprite != null) Destroy(sprite);
        if (directionalMaterial != null) Destroy(directionalMaterial);
        if (refinedFrames != null) foreach (Sprite sprite in refinedFrames) if (sprite != null) Destroy(sprite);
        if (refinedMaterial != null) Destroy(refinedMaterial);
        if (completeFrames != null) foreach (Sprite sprite in completeFrames) if (sprite != null) Destroy(sprite);
        if (walkFrames != null) foreach (Sprite sprite in walkFrames) if (sprite != null) Destroy(sprite);
    }

    private static bool IsDirectionalBackground(Color32 c)
    {
        return c.a < 32 || (c.r > 130 && c.b > 130 && c.g < Mathf.Min(c.r, c.b) * .65f);
    }

    private void BuildDirectionalAtlas(bool actions = false)
    {
        Texture2D atlas = Resources.Load<Texture2D>(actions ? "Sprites/Player/MateoActions_4x8" : "Sprites/Player/MateoDirectional_4x8");
        Shader shader = Shader.Find("Xunjuu/AtlasSprite");
        if (atlas == null || !atlas.isReadable || spriteRenderer == null || shader == null) { directionalFrames=null; return; }
        Color32[] pixels = atlas.GetPixels32();
        directionalFrames = new Sprite[32];
        // Measured gutters: generated sheets are not guaranteed to have uniform row heights.
        float[] edges = {0,237f/1774,449f/1774,665f/1774,882f/1774,1100f/1774,1318f/1774,1537f/1774,1};
        float ppu = atlas.height * (actions ? .104f : .10f) / XunjuuWorldScale.PlayerLocalHeight;
        Collider col = GetComponent<Collider>();
        float bottom = col != null ? transform.InverseTransformPoint(new Vector3(transform.position.x,col.bounds.min.y,transform.position.z)).y : -.5f;
        for (int row=0; row<8; row++) for (int frame=0; frame<4; frame++)
        {
            int x0=Mathf.RoundToInt(frame*atlas.width/4f),x1=Mathf.RoundToInt((frame+1)*atlas.width/4f);
            int y0=atlas.height-Mathf.RoundToInt(edges[row+1]*atlas.height),y1=atlas.height-Mathf.RoundToInt(edges[row]*atlas.height);
            int minY=y1,maxY=y0;
            for(int y=y0;y<y1;y++) for(int x=x0;x<x1;x++)
                if(!IsDirectionalBackground(pixels[y*atlas.width+x])) {minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
            if(minY>maxY) { foreach(var sprite in directionalFrames) if(sprite!=null) Destroy(sprite); directionalFrames=null; return; }
            // Register the body, not the grid cell: the generator shifts the torso
            // between strides and extends scarves/hands outside the body's centre.
            float centre=x0+(x1-x0)*.5f;
            if(!actions)
            {
                float sum=0;int count=0;
                for(int y=minY+Mathf.RoundToInt((maxY-minY)*.37f);y<minY+Mathf.RoundToInt((maxY-minY)*.56f);y++)
                for(int x=x0;x<x1;x++)
                {
                    Color32 c=pixels[y*atlas.width+x];
                    if(!IsDirectionalBackground(c) && c.r>75 && c.r>c.g*1.6f && c.r>c.b*1.25f)
                    {sum+=x;count++;}
                }
                if(count>8)centre=sum/count;
            }
            Rect rect=new Rect(x0,y0,x1-x0,y1-y0);
            float foot=minY-y0;
            float framePpu=actions?ppu:(maxY-minY+1)/XunjuuWorldScale.PlayerLocalHeight;
            directionalFrames[row*4+frame]=Sprite.Create(atlas,rect,new Vector2((centre-x0)/rect.width,(foot-bottom*framePpu)/rect.height),framePpu,0,SpriteMeshType.FullRect);
            (actions?actionRoots:walkRoots)[row*4+frame]=new Vector2(centre-x0,foot);
            directionalFrames[row*4+frame].name=(actions?"Mateo_Action_":"Mateo_Direction_")+row+"_"+frame;
        }
        if(directionalMaterial==null)
        {
            directionalMaterial=new Material(shader){name="Mateo_Pixel_Directional"};
            directionalMaterial.SetFloat("_KeyMode",1);
        }
    }

    public static int DirectionIndex(Vector2 screenDirection)
    {
        return Mathf.RoundToInt(Mathf.Atan2(-screenDirection.x,-screenDirection.y)*Mathf.Rad2Deg/45f+8f)%8;
    }

    private void BuildBalancedWalk()
    {
        var atlas=Resources.Load<Texture2D>("Sprites/Player/MateoWalkBalanced_4x8");
        if(atlas==null || !atlas.isReadable)return;
        var pixels=atlas.GetPixels32();balancedWalk=new Sprite[32];
        var collider=GetComponent<Collider>();
        float bottom=collider!=null?transform.InverseTransformPoint(new Vector3(transform.position.x,collider.bounds.min.y,transform.position.z)).y:-.5f;
        for(int row=0;row<8;row++)for(int frame=0;frame<4;frame++)
        {
            int x0=Mathf.RoundToInt(frame*atlas.width/4f),x1=Mathf.RoundToInt((frame+1)*atlas.width/4f);
            int y0=atlas.height-Mathf.RoundToInt((row+1)*atlas.height/8f),y1=atlas.height-Mathf.RoundToInt(row*atlas.height/8f);
            int minX=x1,maxX=x0,minY=y1,maxY=y0;
            for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)
                if(!IsDirectionalBackground(pixels[y*atlas.width+x])){minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
            if(minX>maxX){foreach(var s in balancedWalk)if(s!=null)Destroy(s);balancedWalk=null;return;}
            // Register the head, not the swinging arm/scarf or moving belt ends.
            float sum=0;int count=0;
            for(int y=minY+Mathf.RoundToInt((maxY-minY)*.78f);y<=maxY;y++)for(int x=minX;x<=maxX;x++)
                if(!IsDirectionalBackground(pixels[y*atlas.width+x])){sum+=x;count++;}
            float center=count>0?sum/count:(minX+maxX)*.5f;
            float ppu=(maxY-minY+1)/XunjuuWorldScale.PlayerLocalHeight;
            Rect rect=new Rect(x0,y0,x1-x0,y1-y0);
            balancedWalk[row*4+frame]=Sprite.Create(atlas,rect,new Vector2((center-x0)/rect.width,(minY-y0-bottom*ppu)/rect.height),ppu,0,SpriteMeshType.FullRect);
            balancedWalk[row*4+frame].name="Mateo_Balanced_"+row+"_"+frame;
        }
    }

    private void BuildSideWalkCorrection()
    {
        var atlas=Resources.Load<Texture2D>("Sprites/Player/MateoWalkSides_4x3");
        if(balancedWalk==null||atlas==null||!atlas.isReadable)return;
        var pixels=atlas.GetPixels32();
        int[] targetRows={2,5,4}; // Profile, rear diagonal, back. Front views remain untouched.
        var bounds=new RectInt[12];
        for(int row=0;row<3;row++)for(int frame=0;frame<4;frame++)
        {
            int x0=frame*atlas.width/4,x1=(frame+1)*atlas.width/4;
            int y0=atlas.height-(row+1)*atlas.height/3,y1=atlas.height-row*atlas.height/3;
            int minX=x1,maxX=x0,minY=y1,maxY=y0;
            for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)
                if(!IsDirectionalBackground(pixels[y*atlas.width+x]))
                {minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);}
            if(minX>maxX)return; // Keep the original set if any replacement cell is empty.
            bounds[row*4+frame]=new RectInt(minX,minY,maxX-minX+1,maxY-minY+1);
        }
        var hull=GetComponent<Collider>();
        float bottom=hull!=null?transform.InverseTransformPoint(new Vector3(transform.position.x,hull.bounds.min.y,transform.position.z)).y:-.5f;
        for(int row=0;row<3;row++)
        {
            float height=0;
            for(int frame=0;frame<4;frame++)height=Mathf.Max(height,bounds[row*4+frame].height);
            float ppu=height/XunjuuWorldScale.PlayerLocalHeight; // One scale for the whole cycle: never stretch each pose to fit.
            for(int frame=0;frame<4;frame++)
            {
                RectInt ink=bounds[row*4+frame];float sum=0;int count=0;
                for(int y=ink.yMin+Mathf.RoundToInt(ink.height*.80f);y<ink.yMax;y++)
                    for(int x=ink.xMin;x<ink.xMax;x++)if(!IsDirectionalBackground(pixels[y*atlas.width+x])){sum+=x;count++;}
                float center=count>0?sum/count:ink.center.x;
                int x0=frame*atlas.width/4,y0=atlas.height-(row+1)*atlas.height/3;
                var rect=new Rect(x0,y0,(frame+1)*atlas.width/4-x0,atlas.height-row*atlas.height/3-y0);
                int index=targetRows[row]*4+frame;
                Destroy(balancedWalk[index]);
                balancedWalk[index]=Sprite.Create(atlas,rect,new Vector2((center-x0)/rect.width,(ink.yMin-y0-bottom*ppu)/rect.height),ppu,0,SpriteMeshType.FullRect);
                balancedWalk[index].name="Mateo_Balanced_"+targetRows[row]+"_"+frame;
            }
        }
        HasSideWalkCorrection=true;
    }

    private void AnimateDirectional()
    {
        bool dead=playerController.IsDead(),attack=playerController.IsAttacking(),jump=playerController.IsJumping();
        Vector3 velocity=body!=null?body.linearVelocity:Vector3.zero;
        Vector3 displacement=transform.position-previousWorldPosition;previousWorldPosition=transform.position;
        float distance=new Vector2(displacement.x,displacement.z).magnitude;
        if(distance>2f)distance=0; // Spawn/teleport is not a stride.
        float speed=distance/Mathf.Max(Time.deltaTime,.001f);
        if(speed>.15f)lastMotionTime=Time.time;
        Vector3 direction=playerController.GetMoveDirection();
        if(!attack && !dead && direction.sqrMagnitude>.01f && Camera.main!=null)
        {
            Vector3 right=Camera.main.transform.right,forward=Camera.main.transform.forward;
            right.y=0;forward.y=0;right.Normalize();forward.Normalize();
            FacingIndex=DirectionIndex(new Vector2(Vector3.Dot(direction,right),Vector3.Dot(direction,forward)));
        }
        int state=dead?4:attack?3:jump?2:Time.time-lastMotionTime<.09f?1:0;
        if(state!=previousRow){actionClock=0;previousRow=state;}
        actionClock+=Time.deltaTime;
        // Canonical front, profile, back and both three-quarter views. Mirror only the
        // matching angle, never a side pose to represent front/back movement.
        bool mirrored=FacingIndex==1 || FacingIndex==3;
        int frame=0;
        if(state==1)
        {
            travelled+=distance;
            // Advance by time, not raw displacement: fast physics steps must not skip
            // the passing poses (0 -> 1 -> 2 -> 3), which caused visible limp walking.
            float cadence=Mathf.Lerp(7.2f,10.5f,Mathf.InverseLerp(.15f,4.5f,speed));
            walkCycleClock+=Time.deltaTime*cadence;
            frame=Mathf.FloorToInt(walkCycleClock)%4;
        }
        else if(state==2)
        {
            walkCycleClock=0;
            frame=(velocity.y>0f)?1:3;
        }
        else if(state==3) frame=actionClock<.1f?0:actionClock<.27f?1:0;
        // Keep the angle even for reactions, instead of switching back to the old profile.
        spriteRenderer.sharedMaterial=directionalMaterial;
        spriteRenderer.sprite=directionalFrames[DirectionRows[FacingIndex]*4+frame];
        activeFrame=frame;
        balancedActive=state==1 && balancedWalk!=null;
        if(balancedActive)
        {
            spriteRenderer.sprite=balancedWalk[BalancedRows[FacingIndex]*4+frame];
            mirrored=FacingIndex==1 || FacingIndex==3 || FacingIndex==6;
        }
        if(directionalActions!=null && state>=2)
        {
            int action=state==4?3:state==2?2:actionClock<.10f?0:actionClock<.27f?1:0;
            spriteRenderer.sprite=directionalActions[DirectionRows[FacingIndex]*4+action];
            activeFrame=action;
        }
        spriteRenderer.flipX=mirrored;
    }

    // Hand landmarks belong to the actual rendered frame, not its silhouette.
    // The small foreground crop lets the fingers cover the equipped handle.
    public Sprite HandGripSprite => spriteRenderer != null && spriteRenderer.sprite != null
        && handPoses.TryGetValue(spriteRenderer.sprite, out HandPose pose) ? pose.foreground : null;

    public Vector3 HandGripWorld
    {
        get
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null
                || !handPoses.TryGetValue(spriteRenderer.sprite, out HandPose pose)) return transform.position;
            Vector2 local = pose.localGrip;
            if(spriteRenderer.flipX)local.x=-local.x;
            if(spriteRenderer.flipY)local.y=-local.y;
            return spriteRenderer.transform.TransformPoint(new Vector3(local.x,local.y,0));
        }
    }

    private void BuildHandPoses()
    {
        var textures = new Dictionary<Texture2D, Color32[]>();
        RegisterHands(directionalFrames, false, textures);
        RegisterHands(directionalActions, true, textures);
        RegisterHands(balancedWalk, false, textures);
    }

    private void RegisterHands(Sprite[] frames, bool actions, Dictionary<Texture2D, Color32[]> textures)
    {
        if (frames == null) return;
        for (int index = 0; index < frames.Length; index++)
        {
            Sprite sprite = frames[index];
            if (sprite == null || !sprite.texture.isReadable) continue;
            if (!textures.TryGetValue(sprite.texture, out Color32[] pixels))
                textures[sprite.texture] = pixels = sprite.texture.GetPixels32();
            int row = index / 4, frame = index % 4;
            Vector2 landmark = HandLandmark(sprite.texture.name, row, frame, actions);
            Vector2 pixel = new Vector2(sprite.rect.x + landmark.x * sprite.rect.width,
                sprite.rect.y + (1f - landmark.y) * sprite.rect.height);
            int radius = Mathf.Max(4, Mathf.RoundToInt(sprite.rect.height * .045f));
            int minX = Mathf.Max((int)sprite.rect.xMin, Mathf.RoundToInt(pixel.x) - radius);
            int maxX = Mathf.Min((int)sprite.rect.xMax - 1, Mathf.RoundToInt(pixel.x) + radius);
            int minY = Mathf.Max((int)sprite.rect.yMin, Mathf.RoundToInt(pixel.y) - radius);
            int maxY = Mathf.Min((int)sprite.rect.yMax - 1, Mathf.RoundToInt(pixel.y) + radius);
            Vector2 sum = Vector2.zero;
            int count = 0, skinMinX = maxX, skinMaxX = minX, skinMinY = maxY, skinMaxY = minY;
            for (int y = minY; y <= maxY; y++) for (int x = minX; x <= maxX; x++)
            {
                Color32 c = pixels[y * sprite.texture.width + x];
                // Warm skin only: ignore the cream sleeve, magenta key and satchel.
                if (c.a < 32 || c.r < 175 || c.g < 95 || c.b > 155
                    || c.r < c.g * 1.16f || c.g < c.b * 1.3f) continue;
                sum += new Vector2(x + .5f, y + .5f); count++;
                skinMinX = Mathf.Min(skinMinX, x); skinMaxX = Mathf.Max(skinMaxX, x);
                skinMinY = Mathf.Min(skinMinY, y); skinMaxY = Mathf.Max(skinMaxY, y);
            }
            if (count > 0) pixel = sum / count;
            var pose = new HandPose { localGrip = (pixel - sprite.rect.position - sprite.pivot) / sprite.pixelsPerUnit };
            if (count > 0)
            {
                // Include the dark fist outline, but never duplicate the whole arm.
                int pad = Mathf.Max(1, Mathf.RoundToInt(sprite.rect.height / 150f));
                var rect = Rect.MinMaxRect(Mathf.Max(sprite.rect.xMin, skinMinX - pad), Mathf.Max(sprite.rect.yMin, skinMinY - pad),
                    Mathf.Min(sprite.rect.xMax, skinMaxX + pad + 1), Mathf.Min(sprite.rect.yMax, skinMaxY + pad + 1));
                pose.foreground = Sprite.Create(sprite.texture, rect, (pixel - rect.position) / rect.size,
                    sprite.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                pose.foreground.name = sprite.name + "_Agarre";
            }
            handPoses[sprite] = pose;
        }
    }

    private static Vector2 HandLandmark(string texture, int row, int frame, bool actions)
    {
        // Pixel measurements of the source sheets (top-left origin). Scale them
        // with the cell so changing import resolution does not change the grip.
        Vector2 point;
        float width = 221.75f;
        float[] heights = {237, 212, 216, 217, 218, 218, 219, 237};
        float height = heights[row];
        if (texture == "MateoWalkSides_4x3")
        {
            width = 443.5f; height = 887f / 3f;
            point = row == 2 ? new[] {new Vector2(130,192),new Vector2(167,200),new Vector2(138,192),new Vector2(184,198)}[frame]
                : row == 4 ? new[] {new Vector2(219,146),new Vector2(222,145),new Vector2(231,144),new Vector2(227,144)}[frame]
                : new[] {new Vector2(232,178),new Vector2(227,172),new Vector2(233,177),new Vector2(234,173)}[frame];
        }
        else if (texture == "MateoWalkBalanced_4x8")
        {
            width = 313.5f; height = 156.75f;
            point = row == 0 ? new[] {new Vector2(180,95),new Vector2(181,94),new Vector2(184,95),new Vector2(185,94)}[frame]
                : row == 7 ? new[] {new Vector2(137,98),new Vector2(146,96),new Vector2(132,97),new Vector2(141,98)}[frame]
                : new Vector2(row == 2 ? 165 : 183, 94);
        }
        else if (actions)
        {
            point = row == 0 ? new[] {new Vector2(131,120),new Vector2(95,119),new Vector2(146,103),new Vector2(129,201)}[frame]
                : row == 2 ? new[] {new Vector2(86,91),new Vector2(48,89),new Vector2(80,74),new Vector2(85,175)}[frame]
                : row == 4 ? new[] {new Vector2(151,84),new Vector2(176,79),new Vector2(154,57),new Vector2(128,182)}[frame]
                : row == 6 ? new[] {new Vector2(143,92),new Vector2(168,81),new Vector2(137,80),new Vector2(125,183)}[frame]
                : row == 7 ? new[] {new Vector2(120,97),new Vector2(170,80),new Vector2(105,113),new Vector2(131,180)}[frame]
                : new[] {new Vector2(151,86),new Vector2(175,80),new Vector2(150,74),new Vector2(140,175)}[frame];
        }
        else
        {
            point = row == 0 ? new[] {new Vector2(143,161),new Vector2(145,156),new Vector2(144,157),new Vector2(144,157)}[frame]
                : row == 2 ? new[] {new Vector2(109,136),new Vector2(80,135),new Vector2(79,135),new Vector2(78,130)}[frame]
                : row == 4 ? new[] {new Vector2(144,141),new Vector2(145,141),new Vector2(146,142),new Vector2(139,140)}[frame]
                : row == 6 ? new[] {new Vector2(125,138),new Vector2(138,133),new Vector2(136,133),new Vector2(124,136)}[frame]
                : row == 7 ? new[] {new Vector2(100,129),new Vector2(111,128),new Vector2(94,129),new Vector2(99,130)}[frame]
                : new[] {new Vector2(152,132),new Vector2(153,134),new Vector2(151,137),new Vector2(148,131)}[frame];
        }
        return new Vector2(point.x / width, point.y / height);
    }

    private void BuildRefinedAtlas()
    {
        Texture2D atlas = Resources.Load<Texture2D>("Sprites/Player/MateoRefined_4x4");
        if (atlas == null || !atlas.isReadable || spriteRenderer == null) return;
        Color32[] pixels = atlas.GetPixels32();
        refinedFrames = new Sprite[16];
        // Measured row gutters of the delivered atlas (rather than assuming an exact generated grid).
        float[] rows = { 0f, 340f/1254f, 646f/1254f, 949f/1254f, 1f };
        float ppu = atlas.height * .222f / XunjuuWorldScale.PlayerLocalHeight;
        Collider collider = GetComponent<Collider>();
        float bottom = collider != null ? transform.InverseTransformPoint(new Vector3(transform.position.x, collider.bounds.min.y, transform.position.z)).y : -.5f;
        for (int i=0;i<16;i++)
        {
            int row=i/4,col=i%4,x0=Mathf.RoundToInt(col*atlas.width/4f),x1=Mathf.RoundToInt((col+1)*atlas.width/4f);
            int y0=atlas.height-Mathf.RoundToInt(rows[row+1]*atlas.height),y1=atlas.height-Mathf.RoundToInt(rows[row]*atlas.height);
            int minX=x1,maxX=x0,minY=y1,maxY=y0;
            for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)
            {
                Color32 c=pixels[y*atlas.width+x];
                int high=Mathf.Max(c.r,Mathf.Max(c.g,c.b)),low=Mathf.Min(c.r,Mathf.Min(c.g,c.b));
                if(c.a<32 || (low>91 && high-low<15))continue;
                minX=Mathf.Min(minX,x);maxX=Mathf.Max(maxX,x);minY=Mathf.Min(minY,y);maxY=Mathf.Max(maxY,y);
            }
            if(minX>maxX || minY>maxY) { refinedFrames=null; return; }
            Rect rect=new Rect(Mathf.Max(x0,minX-2),Mathf.Max(y0,minY-2),Mathf.Min(x1-1,maxX+2)-Mathf.Max(x0,minX-2)+1,Mathf.Min(y1-1,maxY+2)-Mathf.Max(y0,minY-2)+1);
            // Anchor the torso independently from the fist, scarf and changing stride width.
            float torsoX=x0+(x1-x0)*.58f;
            float pivotY=-bottom*ppu/rect.height;
            refinedFrames[i]=Sprite.Create(atlas,rect,new Vector2((torsoX-rect.x)/rect.width,pivotY),ppu,0,SpriteMeshType.FullRect);
            refinedFrames[i].name="Mateo_Refinado_"+i;
        }
        originalMaterial=spriteRenderer.sharedMaterial;
        refinedMaterial=new Material(Shader.Find("Xunjuu/AtlasSprite")){name="Mateo_Recorte_Atlas"};
        spriteRenderer.sharedMaterial=refinedMaterial;
    }

    private void AnimateRefined()
    {
        bool dead=playerController.IsDead(),jumping=playerController.IsJumping(),attack=playerController.IsAttacking();
        Vector3 v=body!=null?body.linearVelocity:Vector3.zero;
        float speed=new Vector2(v.x,v.z).magnitude;
        int state=dead?4:attack?3:jumping?2:speed>.15f?1:0;
        if(state!=previousRow){actionClock=0;previousRow=state;}
        actionClock+=Time.deltaTime;
        int frame=8;
        if(state==4)frame=15;
        else if(state==3)frame=actionClock<.10f?12:actionClock<.25f?13:14;
        else if(state==2)frame=v.y<-.5f?10:11;
        else if(state==1)
        {
            // Distance-based cadence prevents moonwalking when movement is blocked or slowed.
            walkClock+=speed*Time.deltaTime;
            int[] cycle={0,1,2,3,4,5,6,7};
            frame=cycle[Mathf.FloorToInt(walkClock*2.15f)%cycle.Length];
        }
        else frame=8+(Mathf.FloorToInt(actionClock*1.6f)%2);
        spriteRenderer.sharedMaterial=refinedMaterial;
        spriteRenderer.sprite=refinedFrames[frame];
    }

    private Sprite[] BuildGrid(Texture2D texture, int columns, int rows, string prefix)
    {
        if (texture == null || texture.width < columns || texture.height < rows)
            return null;

        int frameWidth = texture.width / columns;
        int frameHeight = texture.height / rows;
        float sheetPixelsPerUnit = pixelsPerUnit * frameHeight / 256f;
        Color32[] pixels = texture.isReadable ? texture.GetPixels32() : null;
        Collider collider = GetComponent<Collider>();
        float bottomOffset = collider != null
            ? Mathf.Abs(transform.InverseTransformPoint(new Vector3(transform.position.x, collider.bounds.min.y, transform.position.z)).y)
            : 0.5f;
        Sprite[] frames = new Sprite[columns * rows];

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                int index = row * columns + column;
                Rect rect = new Rect(
                    column * frameWidth,
                    (rows - 1 - row) * frameHeight,
                    frameWidth,
                    frameHeight);

                if (pixels != null)
                {
                    int minimum = frameHeight, maximum = -1;
                    for (int y = 0; y < frameHeight; y++)
                        for (int x = 0; x < frameWidth; x++)
                            if (pixels[((int)rect.y + y) * texture.width + (int)rect.x + x].a > 16)
                            { minimum = Mathf.Min(minimum, y); maximum = Mathf.Max(maximum, y); }
                    if (maximum >= minimum)
                    { rect.y += minimum; rect.height = maximum - minimum + 1; }
                }

                frames[index] = Sprite.Create(
                    texture,
                    rect,
                    new Vector2(0.5f, Mathf.Clamp01(bottomOffset * sheetPixelsPerUnit / rect.height)),
                    sheetPixelsPerUnit,
                    0,
                    SpriteMeshType.Tight);
                frames[index].name = prefix + "_" + index.ToString("00");
            }
        }

        return frames;
    }

    private bool HasEquippedWeapon()
    {
        OrbitalWeapon[] weapons = GetComponentsInChildren<OrbitalWeapon>(true);
        foreach (OrbitalWeapon weapon in weapons)
        {
            if (weapon != null && weapon.gameObject.activeInHierarchy)
                return true;
        }

        return false;
    }
}
