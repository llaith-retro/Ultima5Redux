﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;

namespace Ultima5Redux
{
    class TalkScript
    {
        /// <summary>
        /// This is a collection of all an NPCs ScriptTalkLabel(s)
        /// </summary>
        protected internal class ScriptTalkLabels
        {
            /// <summary>
            /// A simple list of all the labels
            /// </summary>
            public List<ScriptTalkLabel> Labels { get; }

            /// <summary>
            /// Add a single ScriptTalkLabel
            /// </summary>
            /// <param name="talkLabel"></param>
            public void AddLabel(ScriptTalkLabel talkLabel)
            {
                Labels.Add(talkLabel);
            }

            /// <summary>
            /// Default constructor
            /// </summary>
            public ScriptTalkLabels()
            {
                Labels = new List<ScriptTalkLabel>();
            }
        }

        /// <summary>
        /// A single label for an NPC
        /// Includes all dialog components need for label
        /// </summary>
        protected internal class ScriptTalkLabel
        {
            /// <summary>
            /// All the NPCs question and answers specific to the label
            /// </summary>
            public ScriptQuestionAnswers QuestionAnswers { get;  }
            /// <summary>
            /// The initial line that you will always show when jumping to the label
            /// </summary>
            public ScriptLine InitialLine { get; set; }
            /// <summary>
            /// The default answer if non of the QuestionAnswers are satisfied
            /// </summary>
            public List<ScriptLine> DefaultAnswers { get; set; }
            /// <summary>
            /// The label reference nunber
            /// </summary>
            public int LabelNum { get;  }

            /// <summary>
            /// Add an additional question and answer 
            /// </summary>
            /// <param name="sqa"></param>
            public void AddScriptQuestionAnswer(ScriptQuestionAnswer sqa)
            {
                QuestionAnswers.Add(sqa);
            }

            /// <summary>
            /// Construct the ScriptTalkLabel
            /// </summary>
            /// <param name="labelNum">label number between 0 and 9</param>
            /// <param name="initialLine">The initial line that will always be shown</param>
            /// <param name="defaultAnswers">Default answer if an expected response is not given</param>
            /// <param name="sqa">Script questions and answers</param>
            public ScriptTalkLabel (int labelNum, ScriptLine initialLine, List<ScriptLine> defaultAnswers, ScriptQuestionAnswers sqa)
            {
                QuestionAnswers = sqa;
                InitialLine = initialLine;
                // if one is not provided, then one will be provided for you
                if (defaultAnswers == null)
                {
                    DefaultAnswers = new List<ScriptLine>();
                }
                else
                {
                    DefaultAnswers = DefaultAnswers;
                }
                LabelNum = labelNum;
            }

            /// <summary>
            /// Construct the ScriptTalkLabel
            /// </summary>
            /// <param name="labelNum">label number between 0 and 9</param>
            /// <param name="initialLine">The initial line that will always be shown</param>
            public ScriptTalkLabel(int labelNum, ScriptLine initialLine) : this(labelNum, initialLine, null, new ScriptQuestionAnswers())
            {
            }
        }



        /// <summary>
        /// Collection of questions and answers, makes accessing them much easier
        /// </summary>
        protected internal class ScriptQuestionAnswers
        {
            /// <summary>
            /// All the NPCs question strings and associated answers 
            /// </summary>
            public Dictionary<string, ScriptQuestionAnswer> QuestionAnswers { get; }

            /// <summary>
            /// Based on a user response, return the ScriptQuestionAnswer object
            /// </summary>
            /// <param name="question">question issued by user</param>
            /// <returns>associated QuestionAnswer if one exists</returns>
            public ScriptQuestionAnswer GetQuestionAnswer(string question)
            {
                return QuestionAnswers[question];
            }

            /// <summary>
            /// Get an array of all associated questions
            /// </summary>
            /// <returns></returns>
            public string[] GetScriptQuestions()
            {
                return QuestionAnswers.Keys.ToArray();
            }

            /// <summary>
            /// Default constructor
            /// </summary>
            public ScriptQuestionAnswers()
            {
                QuestionAnswers = new Dictionary<string, ScriptQuestionAnswer>();
            }

            /// <summary>
            /// Get a list of all answer script lines
            /// </summary>
            /// <returns></returns>
            //public List<ScriptLine> GetAnswers()
            //{
            //    List<ScriptLine> answers = new List<ScriptLine>();
            //    foreach (ScriptQuestionAnswer sqa in QuestionAnswers.Values)
            //    {
            //        if (!answers.Contains(sqa.Answer))
            //        {
            //            answers.Add(sqa.Answer);
            //        }
            //    }
            //    return answers;
            //} 

                
            public void Add (ScriptQuestionAnswer sqa)
            {
                if (sqa.questions == null)
                    return;

                foreach (string question in sqa.questions)
                {
                    if (!QuestionAnswers.Keys.Contains(question.Trim()))
                    {
                        QuestionAnswers.Add(question.Trim(), sqa);
                    }
                }
            }

            /// <summary>
            /// Print the object to the console
            /// </summary>
            public void Print()
            {
                Dictionary<ScriptQuestionAnswer, bool> seenAnswers = new Dictionary<ScriptQuestionAnswer, bool>();

                foreach (ScriptQuestionAnswer sqa in QuestionAnswers.Values)
                {
                    if (seenAnswers.ContainsKey(sqa)) continue;
                    seenAnswers.Add(sqa, true);

                    bool first = true;
                    foreach (string question in sqa.questions.ToArray())
                    {
                        if (first) { first = false; Console.Write("Questions: " + question); }
                        else { Console.Write(" <OR> " + question); }
                    }
                    Console.WriteLine("");
                    Console.WriteLine("Answer: " + sqa.Answer.ToString());

                }
            }
        }


        /// <summary>
        /// A single instance of a question and answer for dialog
        /// </summary>
        protected internal class ScriptQuestionAnswer
        {
            public ScriptLine Answer { get; }
            public List<string> questions { get; }

            public ScriptQuestionAnswer(List<string> questions, ScriptLine answer)
            {
                this.questions = questions;
                Answer = answer;
            }
        }



        /// <summary>
        /// Represents a single script component
        /// </summary>
        protected internal class ScriptItem
        {
            /// <summary>
            /// command issued
            /// </summary>
            public TalkCommand Command { get; }
            /// <summary>
            /// Associated string (can be empty)
            /// </summary>
            public string Str { get { return str.Trim(); } }
            /// <summary>
            /// If there is a label, then this is a zero based index
            /// </summary>
            public int LabelNum { get; }

            private string str = string.Empty;

            static public bool IsQuestion(string str)
            {
                // if the string is:
                // 1 to 6 characters
                // AND doesn't contain spaces

                return (str.Trim().Length <= 6 && str.Trim().Length >= 1 && !str.Contains(" "));

                // there are some answers that are capitalized...
                //&& (Str.ToLower() == Str));
            }

            /// <summary>
            /// is this script item a question that the player asks an NPC?
            /// </summary>
            /// <returns></returns>
            public bool IsQuestion()
            {
                return ScriptItem.IsQuestion(Str);
            }

            public ScriptItem(TalkCommand command) : this(command, string.Empty)
            {

            }

            /// <summary>
            /// Creates a label 
            /// </summary>
            /// <param name="command">a GotoLabel or DefineLabel</param>
            /// <param name="nLabelNum">number of the label</param>
            public ScriptItem(TalkCommand command, int nLabelNum)
            {
                Command = command;
                LabelNum = nLabelNum;
            }

            /// <summary>
            /// A talk command with an associated string
            /// </summary>
            /// <param name="command"></param>
            /// <param name="str"></param>
            public ScriptItem(TalkCommand command, string str)
            {
                Command = command;
                this.str = str;
            }
        }

        /// <summary>
        /// Represents a single line of a script
        /// </summary>
        protected internal class ScriptLine
        {
            /// <summary>
            /// a list of all associated ScriptItems, in a particular order
            /// </summary>
            private List<ScriptItem> scriptItems = new List<ScriptItem>();

            /// <summary>
            /// Creates a human readable string for the ScriptLine
            /// </summary>
            /// <returns></returns>
            public override string ToString()
            {
                string scriptLine = string.Empty;

                foreach (ScriptItem item in this.scriptItems)
                {
                    if (item.Command == TalkCommand.PlainString)
                    {
                        scriptLine += item.Str.Trim();
                    }
                    else
                    {
                        if (item.Command == TalkCommand.DefineLabel || item.Command == TalkCommand.GotoLabel)
                        {
                            scriptLine += ("<" + item.Command.ToString() + item.LabelNum.ToString() + ">");
                        }
                        else
                        {
                            scriptLine += ("<" + item.Command.ToString() + ">");
                        }
                    }
                }
                return scriptLine;
            }

            /// <summary>
            /// Is this script line a user input based question
            /// </summary>
            /// <returns>true if it's a question</returns>
            public bool IsQuestion()
            {
                return this.GetScriptItem(0).IsQuestion();
            }

            /// <summary>
            /// Does this line represent the end of all Labels in the NPC talk script (end of script)
            /// </summary>
            /// <returns></returns>
            public bool IsEndOfLabelSection()
            {
                if (GetScriptItem(0).Command == TalkCommand.DefaultMessage && GetScriptItem(1).Command == TalkCommand.Unknown_Enter)
                {
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Does this line represent a new label definition
            /// </summary>
            /// <returns></returns>
            public bool IsLabelDefinition()
            {
                if (GetScriptItem(0).Command == TalkCommand.DefaultMessage && GetScriptItem(1).Command == TalkCommand.DefineLabel)
                {
                    return true;
                }
                return false;
            }

            /// <summary>
            /// Add an additional script item
            /// </summary>
            /// <param name="scriptItem"></param>
            public void AddScriptItem(ScriptItem scriptItem)
            {
                scriptItems.Add(scriptItem);
            }

            /// <summary>
            /// Return the number of current script items
            /// </summary>
            /// <returns>the number of script items</returns>
            public int GetNumberOfScriptItems()
            {
                return scriptItems.Count;
            }

            /// <summary>
            /// Get a script item based on an index into the list
            /// </summary>
            /// <param name="index"></param>
            /// <returns></returns>
            public ScriptItem GetScriptItem(int index)
            {
                Debug.Assert(scriptItems[index] != null);
                return scriptItems[index];
            }

            public List<ScriptLine> SplitIntoSections()
            {
                List<ScriptLine> lines = new List<ScriptLine>();
                lines.Add(new ScriptLine());

                int nSection = 0;
                bool wasIfElseKnowsName = false;
                for (int i = 0; i < GetNumberOfScriptItems(); i++)
                {
                    ScriptItem item = GetScriptItem(i);
                    // Code A2 appears to denote the beginning of a new section, so we split it
                    if (item.Command == TalkCommand.Unknown_CodeA2)
                    {
                        nSection++;
                        lines.Add(new ScriptLine());
                    }
                    // if there is a IfElse branch for the Avatar's name then we add a new section, save the ScriptItem
                    else if (item.Command == TalkCommand.IfElseKnowsName)
                    {
                        //wasIfElseKnowsName = true;
                        nSection++;
                        lines.Add(new ScriptLine());
                        lines[nSection].AddScriptItem(item);
                    }
                    //////// THIS IS WAY MORE COMPLICATED
                    //// how do we detect that there is section split for an IfElse. If it's a gotolabel, then it's easy
                    //// if we see an A2, does that mean we need to wait for another before splitting it?
                    //else if (wasIfElseKnowsName && )
                    else if (item.Command == TalkCommand.DefineLabel)
                    {
                        //wasIfElseKnowsName = false;
                        nSection++;
                        lines.Add(new ScriptLine());
                        lines[nSection].AddScriptItem(item);
                    }
                    else
                    {
                        lines[nSection].AddScriptItem(item);
                    }
                }
                return (lines);
            }

            /// <summary>
            /// Determines if a particular talk command is present in a script line
            /// <remarks>This particularly helpful when looking for looking for <AvatarName></remarks>
            /// </summary>
            /// <param name="command">the command to search for</param>
            /// <returns>true if it's present, false if it isn't</returns>
            public bool ContainsCommand(TalkCommand command)
            {
                for (int nItem = 0; nItem < scriptItems.Count; nItem++)
                {
                    if (scriptItems[nItem].Command == command)
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Is the command only a simple or dynamic string?
        /// </summary>
        /// <param name="command">the command to evaluate</param>
        /// <returns>true if it is a string command only</returns>
        static public bool IsStringOnlyCommand (TalkCommand command)
        {
            if (command == TalkCommand.PlainString || command == TalkCommand.AvatarsName || command == TalkCommand.NewLine || command == TalkCommand.Rune)
                return true;

            return false;
        }

        /// <summary>
        /// Specific talk command
        /// </summary>
        public enum TalkCommand
        {
            PlainString = 0x00, AvatarsName = 0x81, EndCoversation = 0x82, Pause = 0x83, JoinParty = 0x84, Gold = 0x85, Change = 0x86, Or = 0x87, AskName = 0x88, KarmaPlusOne = 0x89,
            KarmaMinusOne = 0x8A, CallGuards = 0x8B, IfElseKnowsName = 0x8C, NewLine = 0x8D, Rune = 0x8E, KeyWait = 0x8F, DefaultMessage = 0x90, Unknown_CodeA2 = 0xA2, Unknown_Enter = 0x9F, GotoLabel = 0xFD, DefineLabel = 0xFE,
            Unknown_FF = 0xFF
        };
//        public enum TalkCommand
//        {
//            PlainString = 0x00, AvatarsName = 0x81, EndCoversation = 0x82, Pause = 0x83, JoinParty = 0x84, Gold = 0x85, Change = 0x86, Or = 0x87, AskName = 0x88, KarmaPlusOne = 0x89,
//            KarmaMinusOne = 0x8A, CallGuards = 0x8B, SetFlag = 0x8C, NewLine = 0x8D, Rune = 0x8E, KeyWait = 0x8F, DefaultMessage = 0x90, Unknown_CodeA2 = 0xA2, Unknown_Enter = 0x9F, GotoLabel = 0xFD, DefineLabel = 0xFE,
//            Unknown_FF = 0xFF
//        };

        /// <summary>
        ///  the minimum talk code for labels (in .tlk files)
        /// </summary>
        public const byte MIN_LABEL = 0x91;

        /// <summary>
        /// the maximum talk code for labels (in .tlk files)
        /// </summary>
        public const byte MAX_LABEL = 0x91 + 0x0A;

        /// <summary>
        /// total number of labels that are allowed to be defined
        /// </summary>
        public const int TOTAL_LABELS = 0x0A;

        // All of the ScriptLines
        private List<ScriptLine> scriptLines = new List<ScriptLine>();

        /// <summary>
        /// Script talk labels contain all the labels, their q&a and default responses
        /// </summary>
        private ScriptTalkLabels scriptTalkLabels = new ScriptTalkLabels();
        
        /// <summary>
        /// Non label specific q&a 
        /// </summary>
        private ScriptQuestionAnswers scriptQuestionAnswers = new ScriptQuestionAnswers();

        // tracking the current script line
        private ScriptLine currentScriptLine = new ScriptLine();

        private const int endBaseIndexes = 4; // the end index for the base (TalkConstants)
        //private int endTextIndexes;

        /// <summary>
        /// The default script line offsets for the static responses
        /// </summary>
        public enum TalkConstants { Name = 0, Description, Greeting, Job, Bye }

        /// <summary>
        /// Build the initial TalkScrit
        /// </summary>
        public TalkScript()
        {
            // let's add it immediately instead of waiting for someone to commit it
            // note; this will fail if the currentScriptLine is not a reference - but I'm pretty sure it is
            scriptLines.Add(currentScriptLine);
        }

        /// <summary>
        /// Move to the next line in the script (for adding new content)
        /// </summary>
        public void NextLine()
        {
            currentScriptLine = new ScriptLine();
            scriptLines.Add(currentScriptLine);
        }


        /// <summary>
        /// After adding all elements, this will process the script into a more readable format
        /// </summary>
        public void InitScript()
        {
            // we keep track of the index into the ScriptLines all the way through the entire method
            int nIndex = endBaseIndexes + 1;

            // have we encountered a label yet?

            bool labelEncountered = false;

            string question;
            
            // repeat through the question/answer components until we hit a label - then we know to move onto the label section
            do
            {
                List<string> currQuestions = new List<string>();
                ScriptLine line = scriptLines[nIndex];

                // if we just hit a label, then it's time to jump out of this loop and move onto the label reading loop
                if (line.GetScriptItem(0).Command == TalkCommand.DefaultMessage)
                {
                    labelEncountered = true;
                    break;
                }

                // first time around we KNOW there is a first question, all NPCs have at least one question
                question = line.GetScriptItem(0).Str;

                // dumb little thing - there are some scripts that have the same keyword multiple times
                // the game favours the one it sees first (see "Camile" in West Brittany as an example)
                if (!scriptQuestionAnswers.QuestionAnswers.ContainsKey(question))
                    currQuestions.Add(question);

                // if we peek ahead and the next command is an <or> then we will just skip it and continue to add to the questions list
                while (scriptLines[nIndex + 1].ContainsCommand(TalkCommand.Or))
                {
                    nIndex += 2;
                    line = scriptLines[nIndex];
                    question = line.GetScriptItem(0).Str;
                    // just in case they try to add the same question twice - this is kind of a bug in the data since the game just favours the first question it sees
                    if (!scriptQuestionAnswers.QuestionAnswers.ContainsKey(question))
                    {
                        currQuestions.Add(question);
                    }
                }

                ScriptLine nextLine = scriptLines[nIndex + 1];
                scriptQuestionAnswers.Add(new ScriptQuestionAnswer(currQuestions, nextLine));
                nIndex+=2;
            } while (labelEncountered == false);

            // time to process labels!! the nIndex that the previous routine left with is the beginning of the label section
            int count = 0;
            do // begin the label processing loop - pretty sure this is dumb and doesn't do anything - but everything messes up when I remove it
            {
                // this is technically a loop, but it should never actually loop. Kind of dumb, but fragile 
                Debug.Assert(count++ == 0);

                ScriptLine line = scriptLines[nIndex];
                ScriptLine nextLine;

                // if there are two script items, and those two script items identify an end of label section then let's break out
                // this should only actually occur if there are no labels at all
                if (line.GetNumberOfScriptItems() == 2 && line.IsEndOfLabelSection())
                {
                    // all done. we either had no labels or reached the end of them
                    // assert that we are on the last line of the script
                    Debug.Assert(nIndex == scriptLines.Count - 1);
                    break;
                }


                // i expect that this line will always indicate a new label is being defined
                Debug.Assert(line.GetScriptItem(0).Command == TalkCommand.DefaultMessage);

                // I don't like this, it's inelegant, but it works...
                // at this point we know:
                // This is a multi line message 
                bool nextCommandDefaultMessage = false;
               
                do // called for each label #
                {
                    // Debug code for narrowing down to a single NPC
                    //if (scriptLines[0].GetScriptItem(0).Str.ToLower().Trim() == "sutek".ToLower())
                    //if (scriptLines[0].GetScriptItem(0).Str.ToLower().Trim() == "sir arbuthnot")
                    //{
                    //        Console.WriteLine("AH");
                    //}

                    line = scriptLines[nIndex];

                    // let's make sure there are actually labels to look at
                    if (line.IsEndOfLabelSection())
                    {
                        nextCommandDefaultMessage = true;
                        break;
                    }

                    // create the shell for the label
                    ScriptTalkLabel scriptTalkLabel = new ScriptTalkLabel(line.GetScriptItem(1).LabelNum, line);

                    // save the new label to the label collection
                    scriptTalkLabels.AddLabel(scriptTalkLabel);

                    // it's a single line only, so we skip this tom foolery below
                    if (scriptLines[nIndex + 1].GetScriptItem(0).Command == TalkCommand.DefaultMessage)
                    {
                        // do nothing, the ScriptTalkLabel will simply have no DefaultAnswer indicating that only the primary label line is read

                        nIndex++;
                        continue;
                    }

                    // with a single answer below the label, we will always use the default answer
                    ScriptLine defaultAnswer = scriptLines[++nIndex];
                    scriptTalkLabel.DefaultAnswers.Add(defaultAnswer);

                    // it's a default only answer, and no additional line of dialog, then we skip this tom foolery below 
                    if (scriptLines[nIndex + 1].GetScriptItem(0).Command == TalkCommand.DefaultMessage)
                    {
                        nIndex++;
                        continue;
                    }

                    do // go through the question/answer and <or>
                    {
                        // Debug code to stop at given index
                        //if (nIndex == 22) { Console.WriteLine(""); }

                        List<string> currQuestions = new List<string>();
                        // if the next line is an <or> then process the <or> 
                        if (scriptLines[nIndex + 2].ContainsCommand(TalkCommand.Or))
                        {
                            while (scriptLines[nIndex + 2].ContainsCommand(TalkCommand.Or))
                            {
                                line = scriptLines[nIndex + 1];
                                Debug.Assert(line.IsQuestion());
                                question = line.GetScriptItem(0).Str;
                                // just in case they try to add the same question twice - this is kind of a bug in the data since the game just favours the first question it sees
                                if (!scriptQuestionAnswers.QuestionAnswers.ContainsKey(question))
                                {
                                    currQuestions.Add(question);
                                }
                                nIndex += 2;
                            }
                            line = scriptLines[++nIndex];
                            Debug.Assert(line.IsQuestion());
                            question = line.GetScriptItem(0).Str;
                            // just in case they try to add the same question twice - this is kind of a bug in the data since the game just favours the first question it sees
                            if (!scriptQuestionAnswers.QuestionAnswers.ContainsKey(question))
                            {
                                currQuestions.Add(question);
                            }
                        }
                        // is this a question that the player would ask an NPC?
                        else if (scriptLines[nIndex + 1].GetScriptItem(0).IsQuestion())
                        {
                            // get the Avater's response line
                            line = scriptLines[++nIndex];

                            question = line.GetScriptItem(0).Str;
                            Debug.Assert(ScriptItem.IsQuestion(question));
                            currQuestions.Add(question);
                        }
                        // the NPC has tricked me - this is a second line of dialog for the given 
                        /// that dasterdly LB has put an extra response line in....
                        else //if (scriptLines[nIndex + 1].GetScriptItem(0).Str.Trim().Length > 4)
                        {
                            line = scriptLines[++nIndex];
                            Debug.Assert(!line.IsQuestion());
                            scriptTalkLabel.DefaultAnswers.Add(line);
                            nIndex++;
                            // let's make double sure that we only have a single additional line of text 
                            Debug.Assert(scriptLines[nIndex].GetScriptItem(0).Command == TalkCommand.DefaultMessage);

                            nextLine = scriptLines[nIndex];
                            continue;
                        }

                        // get your answer and store it
                        ScriptLine npcResponse = scriptLines[++nIndex];
                        // we are ready to create a Q&A object and add it the label specific Q&A script
                        scriptTalkLabel.AddScriptQuestionAnswer(new ScriptQuestionAnswer(currQuestions, npcResponse));

                        // we are at the end of the label section of the file, so we are done.
                        nextLine = scriptLines[++nIndex];

                        // does the next line indicate end of all of the label sections, then let's get out of this loop
                        if (nextLine.IsEndOfLabelSection())
                        {
                            nIndex--;
                            nextCommandDefaultMessage = true;
                            break;
                        }
                        // is the next line a label definition? is so, let's exit this label and move on
                        if (!nextLine.IsLabelDefinition())
                        {
                            nIndex--;
                            continue;
                        }
       
                    // while we know the next line is not a new label or end of label, then let's keep reading by moving to our next loop
                    } while (nextLine.GetScriptItem(0).Command != TalkCommand.DefaultMessage);
                    // while we haven't encountered an end of label section 
                } while (!nextCommandDefaultMessage);

                nIndex++;
            // while we haven't read every last line, then let's keep reading
            } while (nIndex < (scriptLines.Count - 1));
        }

        /// <summary>
        /// Get the script line based on the specified Talk Constant allowing to quickly access "name", "job" etc.
        /// This is not compatible with Labels
        /// </summary>
        /// <param name="talkConst">name, job etc.</param>
        /// <returns>The corresponding single ScriptLine</returns>
        public ScriptLine GetScriptLine(TalkConstants talkConst)
        {
            return (scriptLines[(int)talkConst]);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="nLabel"></param>
        /// <returns></returns>
        /*public ScriptLine GetScriptLineLabel(int nLabel)
        {
            foreach (ScriptLine line in scriptLines)
            {
                ScriptItem item = line.GetScriptItem(0);
                if (item.Command == TalkCommand.DefineLabel && item.LabelNum == nLabel)
                {
                    return line;
                }
            }
            throw new Exception("You requested a script label that doesn't exist");
        }*/


        /// <summary>
        /// Add a talk label. 
        /// </summary>
        /// <param name="talkCommand">Either GotoLabel or DefineLabel</param>
        /// <param name="nLabel">label # (0-9)</param>
        public void AddTalkLabel(TalkCommand talkCommand, int nLabel)
        {
            if (nLabel < 0 || nLabel > TOTAL_LABELS)
            {
                throw new Exception("Label Number: " + nLabel.ToString() + " is out of range");
            }

            if (talkCommand == TalkCommand.GotoLabel || talkCommand == TalkCommand.DefineLabel)
            {
                currentScriptLine.AddScriptItem(new ScriptItem(talkCommand, nLabel));
                //System.Console.Write("<" + (talkCommand.ToString() + " " + nLabel + ">"));
            }
            else 
            {
                throw new Exception("You passed a talk command that isn't a label! ");
            }
        }

        /// <summary>
        /// Add to the current script line, but no string associated
        /// For example: STRING <NEWLINE><AVATARNAME>
        /// </summary>
        /// <param name="talkCommand"></param>
        public void AddTalkCommand(TalkCommand talkCommand)
        {
            //System.Console.Write("<" + (talkCommand.ToString() + ">"));

            currentScriptLine.AddScriptItem(new ScriptItem(talkCommand, string.Empty));
        }

        /// <summary>
        /// Add to the current script line
        /// For example: STRING <NEWLINE><AVATARNAME>
        /// </summary>
        /// <param name="talkCommand"></param>
        /// <param name="talkStr"></param>
        public void AddTalkCommand(TalkCommand talkCommand, string talkStr)
        {
            if (talkCommand == TalkCommand.PlainString)
            {
                currentScriptLine.AddScriptItem(new ScriptItem(talkCommand, talkStr));
            }
            else
            {
                currentScriptLine.AddScriptItem(new ScriptItem(talkCommand));
            }
        }

        /// <summary>
        /// Prints the script out using all of the advanced ScriptLine, ScriptLabel and ScriptQuestionAnswer(s) objects, instead of just text
        /// </summary>
        public void PrintComprehensiveScript()
        {
            Console.WriteLine("---- BEGIN NEW SCRIPT -----");
            Console.WriteLine("Name: " + this.GetScriptLine(TalkConstants.Name).ToString());
            Console.WriteLine("Description: " + this.GetScriptLine(TalkConstants.Description).ToString());
            Console.WriteLine("Greeting: " + this.GetScriptLine(TalkConstants.Greeting).ToString());
            Console.WriteLine("Job: " + this.GetScriptLine(TalkConstants.Job).ToString());
            Console.WriteLine("Bye: " + this.GetScriptLine(TalkConstants.Bye).ToString());
            Console.WriteLine("");

            scriptQuestionAnswers.Print();
            Console.WriteLine("");

            // enumerate the labels and print their scripts
            foreach (ScriptTalkLabel label in this.scriptTalkLabels.Labels)
            {
                Console.WriteLine("Label #: " + label.LabelNum.ToString());
                Console.WriteLine("Initial Line: " + label.InitialLine);
                if (label.DefaultAnswers.Count > 0)
                {
                    foreach (ScriptLine line in label.DefaultAnswers)
                    {
                        Console.WriteLine("Default Line(s): " + line);
                    }
                    label.QuestionAnswers.Print();
                }
            }
        }

        /// <summary>
        /// Print the script out to the console
        /// This is the raw print routine that uses the relatively raw script data
        /// </summary>
        public void PrintScript()
        {
            foreach (ScriptLine line in scriptLines)
            {
                for (int nItem = 0; nItem < line.GetNumberOfScriptItems(); nItem++)
                {
                    ScriptItem item = line.GetScriptItem(nItem);

                    if (item.Command == TalkCommand.PlainString)
                    {
                        System.Console.Write(item.Str);
                    }
                    else
                    {
                        if (item.Command == TalkCommand.DefineLabel || item.Command == TalkCommand.GotoLabel)
                        {
                            System.Console.Write("<" + item.Command.ToString() + item.LabelNum.ToString() + ">");
                        }
                        else
                        {
                            System.Console.Write("<" + item.Command.ToString() + ">");
                        }
                    }
                }
            }
        }
}
}
