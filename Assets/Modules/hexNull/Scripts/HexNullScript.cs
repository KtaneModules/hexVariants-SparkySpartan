using KeepCoding;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class HexNullScript : ModuleScript
{
    /*
    I know you're going to copy over this basic template for new modules, so heres a reminder that you shouldn't get rid of:
    MAKE SURE TO ATTACH mod.bundle TO MODULE PREFABS AND ALL SOUNDS! If hexVariants 2.0 happens again, there is no one to blame but yourself.
    (+ a thank you to emik and river for making sure the time of which issues were happening was only a day long)
    */


    public KMSelectable Screen;
    public KMSelectable[] Buttons;

    public Renderer ScreenRender;
    public Renderer[] ButtonRenders;
    public SpriteRenderer LeftPlane, MiddlePlane, RightPlane;

    public Sprite[] EntityTextures;
    public Sprite[] BinaryTextures;
    public Sprite TransparentTexture;

    private Routine<int> routine;

    // This is the simplest way I could think to add numbers together - purpose explained further in "GenerateSolution"
    private static int[] _binaryTable = new int[8]
    {
        11000001,
        11100000,
        01110000,
        00111000,
        00011100,
        00001110,
        00000111,
        10000011
    };

    // Related to above - made to pull unique numbers in a random order - though the shuffle happens later
    private int[] _indexes = Enumerable.Range(0, 8).ToArray();

    // 4 numbers between 0 and 7 - it tells you which of the binary table values were pulled!
    private int[] _entitySolution = new int[4];

    // This will be a binary number that always has some combination of four 0's and four 1's
    internal int[] solution = new int[8];

    // This will be a binary number that can be any value, because that's how submissions work
    internal int[] submission = new int[8];

    // This prevents the same position from coming up
    private List<int> _queue = new List<int> { };

    // One time boolean: Just used when the module is loaded for the first time
    private bool _moduleActive = false;

    // Multi-use boolean: Disables all interactions when submitting an answer or listening to the entity
    private bool _moduleDisabled = false;

    // Used to make cool visuals for the module
    private float _opacity = 0f;
    private double _solveAnimation = 0;
    private double _solveAnimationIncrement = 0;

    private int _location = 0;
    private int _depth = 0;
    internal int entityPosition = 0;
    private int _interactions = 0;

    // Use this for initialization
    private void Start ()
    {
        routine = new Routine<int>(EntityCollision, this);

        Screen.Assign(onInteract: HandleScreen);
        Buttons.Assign(onInteract: HandleButtons);

        GenerateSolution();
        CreateQueue();

        Cache(GetComponentsInChildren<TextMesh>)[0].text = "hexNull\nIdle";
    }
	
	// Update is called once per frame
	private void FixedUpdate ()
    {
        // just like in hexOrbits, not that much happens here - but damn, does this feature work good for having objects fade out of existance
        if (_opacity > 0 && _solveAnimationIncrement == 0)
        {
            _opacity -= 0.01f;
            LeftPlane.color = new Color { r = 255, b = 255, g = 255, a = _opacity };
            MiddlePlane.color = new Color { r = 255, b = 255, g = 255, a = _opacity };
            RightPlane.color = new Color { r = 255, b = 255, g = 255, a = _opacity };
        }

        //okay, this solve animation is incredibly jank, but in comparison to hexOrbits, it looks sick
        else if (_solveAnimationIncrement > 0)
        {
            _solveAnimation += _solveAnimationIncrement;
            if (_solveAnimation > 10) {
                _solveAnimation -= 10;
                _solveAnimationIncrement += 0.05;

                LeftPlane.sprite = EntityTextures[Rnd.Range(0, 4)];
                MiddlePlane.sprite = EntityTextures[Rnd.Range(0, 4)];
                RightPlane.sprite = EntityTextures[Rnd.Range(0, 4)];
            }
            
        }
	}

    private void HandleScreen()
    {
        if (!_moduleDisabled)
        {
            _interactions++;
            _location = 0;
            _depth = 0;

            // remember, ternary operators look cool, and you should use them whenever possible
            Cache(GetComponentsInChildren<TextMesh>)[0].text = _moduleActive ? "hexNull\nIteration reset." : "hexNull\nIteration loaded.";

            // this shouldn't be able to pull positions that have been seen recently -

            entityPosition = (_queue[0]);
            _queue.RemoveAt(0);
            CreateQueue();

            // these lines give you the exact position of the entity whenever you generate a new position
            PlaySound(entityPosition <= 3 ? "Null0" : "Null1");
            LeftPlane.sprite = TransparentTexture;
            MiddlePlane.sprite = EntityTextures[entityPosition % 4];
            RightPlane.sprite = TransparentTexture;

            _opacity = 1f;
            Log("Interaction {0} - Entity is in position {1}.".Form(_interactions, entityPosition));

            _moduleActive = true;
        }
    }


    private void HandleButtons(int arg1)
    {
        // i have a confession to make: although emik has taught me how to use and make a binary tree, hexNull actually does not contain any binary trees within the code
        // i'll explain as the comments go - sorry if you feel betrayed, i'll make up for it another time!

        if (_moduleActive && !_moduleDisabled && _depth < 3)
        {
            _location *= 2;

            // did we press the right button? if so, add 1 to the location, otherwise, dont
            // this perfectly represents binary tree logic, and it's straight up just easier to both run and understand than an actual binary tree
            _location += arg1 == 1 ? 1 : 0;

            _depth++;
            if (_depth == 3)
            {
                if (_location == entityPosition)
                {
                    Cache(GetComponentsInChildren<TextMesh>)[0].text = "hexNull\nIteration corrupted.";
                    routine.Start(arg1);
                }
                else
                {
                    submission[_location] = (submission[_location] + 1) % 2;
                    submission[entityPosition] = (submission[entityPosition] + 1) % 2;
                    Cache(GetComponentsInChildren<TextMesh>)[0].text = "hexNull\nWritten to nodes {0} and {1}.".Form(_location, entityPosition);
                    PlaySound("NullWrite");
                    Log("Data written on interaction {0} - Player is in position {1}, Entity is in position {2} - current submission value is {3}.".Form(_interactions, _location, entityPosition, submission.Join("")));
                }

            }
            else
            {
                PlaySound("NullStep");
                Cache(GetComponentsInChildren<TextMesh>)[0].text = "hexNull\nIteration in progress...";
            }
        }
    }

    // this cipher is inspired by "chicory - a colourful tale", but in a "gas station sushi" kind of way (https://twitter.com/AllM_XN/status/1270995209770303488)
    // (that is to say: it isn't, but i went on a chain of ideas from that point to reach "that coloured block puzzle used to unlock flopside in super paper mario")
    private void GenerateSolution()
    {
        _indexes = _indexes.OrderBy(a => Rnd.Range(0, 8)).ToArray();
        int tempSolution = _binaryTable[_indexes[0]] + _binaryTable[_indexes[1]] + _binaryTable[_indexes[2]];

        // this code is designed to stop exactly when a break is called - if we run through all the possible combinations without succeeding, throw an exception (shouldn't be possible, but just in case)
        for (int i = 3; ; i++)
        {
            if (i > 7)
            {
                Module.Solve();
                throw new IndexOutOfRangeException();
            }
            // there's a lot going on here, so let's break it down:
            // a candidate for the final binary table number is added, creating an 8-digit number containing digits between 0 and 3
            // the number is converted to string, and then converted to a character array
            // linq's select takes each object within the array, gets its numeric value, returns all of these values, and passes the number back to an array
            int[] returnSolution = (tempSolution + _binaryTable[_indexes[i]]).ToString().ToCharArray().Select(x => (int)Char.GetNumericValue(x) % 2).ToArray();

            // modulo each number in the array by two, and count the amount of 1's - if the count is exactly 4, we have a solution!
            // after 3 binary table additions, there will be exactly either 3, 5, or 7 (1)'s - every board can reach a 4 (1)'s state from one move, unconditionally
            if (returnSolution.Where(j => j % 2 == 1).Count() == 4 && returnSolution.Length == 8)
            {
                Log("hexNull - ERROR - Solving method not found. Searching for address ({0}).".Form(returnSolution.Join("")));
                Log("This adress was generated by modifying index positions {0}, {1}, {2}, and {3}.".Form(_indexes[0], _indexes[1], _indexes[2], _indexes[i]));
                Log("Note: All logged messages have their indexes start at 0.");
                solution = returnSolution;
                _entitySolution = new int[4] { _indexes[0], _indexes[1], _indexes[2], _indexes[i] };
                break;
            }
        }
    }

    private IEnumerator EntityCollision(int arg1)
    {
        Cache(GetComponentsInChildren<TextMesh>)[0].text = "";
        _moduleDisabled = true;
        if (submission.Max() == 0)
        {
            // an entity presented the solution of hexNull to the defuser. this is what happened to their brain.
            Log("Player interacted with entity on interaction {0}. Submission array was 0000000 - solution provided.".Form(_interactions));
            PlaySound("NullCommunication");

            yield return new WaitForSecondsRealtime(1);
            EntityDisplay(0);
            yield return new WaitForSecondsRealtime(2);
            EntityDisplay(1);
            yield return new WaitForSecondsRealtime(2);
            EntityDisplay(2);
            yield return new WaitForSecondsRealtime(2);
            EntityDisplay(3);
            yield return new WaitForSecondsRealtime(3);

            _moduleDisabled = false;
            Cache(GetComponentsInChildren<TextMesh>)[0].text = "hexNull\nAwaiting new input.";
        }
        else
        {
            PlaySound("NullAnticipation");
            Cache(GetComponentsInChildren<TextMesh>)[0].text = "hexNull\nVerifying data...";
            yield return new WaitForSecondsRealtime(2.5f);

            if (submission.SequenceEqual(solution))
            {
                Log("Player interacted with entity on interaction {0}. Submission array was {1} against {2} - Solved.".Form(_interactions, submission.Join(""), solution.Join("")));
                PlaySound("NullSolve");
                Module.Solve();

                LeftPlane.color = new Color { r = 255, b = 255, g = 255, a = 1f };
                MiddlePlane.color = new Color { r = 255, b = 255, g = 255, a = 1f };
                RightPlane.color = new Color { r = 255, b = 255, g = 255, a = 1f };

                _solveAnimation += 10;
                _solveAnimationIncrement += 1;
                Cache(GetComponentsInChildren<TextMesh>)[0].text = "";

                yield return new WaitForSecondsRealtime(10);

                _solveAnimation = 0;
                _solveAnimationIncrement = 0;

                LeftPlane.color = new Color { r = 255, b = 255, g = 255, a = _opacity };
                MiddlePlane.color = new Color { r = 255, b = 255, g = 255, a = _opacity };
                RightPlane.color = new Color { r = 255, b = 255, g = 255, a = _opacity };

                Cache(GetComponentsInChildren<TextMesh>)[0].text = "hexNull\nMemory restored.";
            }
            else
            {
                Log("Player interacted with entity on interaction {0}. Submission array was {1} against {2} - Strike.".Form(_interactions, submission.Join(""), solution.Join("")));
                PlaySound("NullStrike");
                Module.Strike();
                Cache(GetComponentsInChildren<TextMesh>)[0].text = "hexNull\n{0} not found.".Form(submission.Join(""));
                submission = new int[8];
                _moduleDisabled = false;
                
            }
        }
    }


    // on display - me holding together my modules with scotch tape
    private void EntityDisplay(int input)
    {
        // a thank you to Blananas for telling me about the existance of sprites, and also pointing me towards "Assorted Arrangement" to learn from
        LeftPlane.sprite = BinaryTextures[(int)Math.Floor((double)_entitySolution[input] / 4)];
        MiddlePlane.sprite = BinaryTextures[(int)Math.Floor((double)_entitySolution[input] / 2) % 2];
        RightPlane.sprite = BinaryTextures[(int)Math.Floor((double)_entitySolution[input]) % 2];
        _opacity = 1.5f;
    }

    private void CreateQueue()
    {
        while (_queue.Count < 5)
        {
            int i = Rnd.Range(0, 8);
            if (!_queue.Contains(i))
            {
                _queue.Add(i);
            }
        }
    }

}

/*
if you're reading this, hi! i'm not exactly what you'd call a professional programmer. (both in the sense of writing good code and writing comments with consistant formatting)
this project has been a thorn in my mind for the past few months - hopefully getting this done will be the thing that unlocks my ability to make more modules!

hexNull is an intepretation of the "Nullification Project" within hexyl's backstory - if you've ever seen zero escape, it's kind of like that, but without any of the people
the purpose of the project is to find a definitive answer to "how much can you alter someone's mind without them questioning it?" (null is a codename for all participants!)
every time a possible timeline of events plays out, everything returns back to its initial state, with null and the entity both losing most of their memories in the process
normally, every timeline would need to be seen in order to escape - but we're here to solve a hexOS-brand module, not decay (no shade, it's literally intended to be tedious)
so instead, the normal exit has been removed, and you need to write binary to escape with the power of reading squares - and that's hexNull!

here is some bonus lore if you both like studying code and reading about lore for some strange reason:

null is not one person, and is instead one of the many within hexyl's collective who has actively chosen to have their memory temporarily wiped for the nullification project
depending on the day, it could be anyone, possibly even hexyl themself! - their appearance is always the same though, monochrome white suit, no traits of organic or inorganic

the entity on the other hand, is not a person at all, but instead an artificial intelligence specificially programmed to help null along their journey towards escape
they're also programmed to annihilate null on sight, which means many pathways end with null finding out the secrets of the project, and having that memory locked away
full simulations of the project take ~6 hours total, it works out eventually - for appearance, imagine a completely freestyle shapeshifting (and colour-shifting) scientist
*/
