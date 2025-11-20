# Agile Interviewing: Engineering Clarity Instead of Stress

<!--category-- Interviewing, Software Development, Agile -->
<datetime class="hidden">2025-11-20T14:30</datetime>

# Introduction

Most technical interviews are broken. Not "could be better" broken—**fundamentally broken**. They measure stress response, not engineering skill. They favour candidates with abundant free time over candidates with actual ability. They create anxiety that actively prevents people from demonstrating what they know.

And here's the thing: **we know this**. Every developer who's been through a whiteboard interview where they forgot how to reverse a string, every senior engineer who's drawn a blank on basic algorithms they use daily, every brilliant coder who's bombed because they were thinking about the camera pointed at them instead of the problem—we all know the current approach is rubbish.

Yet we keep doing it. Why?

Because we haven't collectively agreed on something better. This article is about that better approach—one I've refined over years of interviewing at Microsoft, Dell, and various startups. One that respects candidates as human beings, reveals actual engineering ability, and creates lasting positive reputation even among those you don't hire.

This isn't just "being nice." It's **efficient** and **accurate**—especially when aligned with agile principles and cognitive accessibility. Helpful for neurodivergent minds, yes, but beneficial for everyone.

[TOC]

# The Problem With Most Technical Interviews

## Brain Teasers: Measuring Cleverness, Not Engineering

"How would you move Mount Fuji?" "How many golf balls fit in a 747?" "Why are manhole covers round?"

These questions were fashionable at Microsoft in the 90s. They were nonsense then; they're nonsense now. They measure:
- Whether you've heard the puzzle before
- How well you perform under arbitrary pressure
- Your ability to guess what the interviewer wants to hear

They don't measure whether you can design a robust API, debug a production issue, or work effectively with a team.

I've never hired someone based on a brain teaser who turned out to be brilliant. I have hired people who bombed brain teasers who turned out to be brilliant. The correlation is noise.

## Algorithm Tests: Optimising for Computer Science Exams

"Implement a red-black tree." "Write an algorithm to detect cycles in a graph." "Reverse a linked list on a whiteboard."

Unless you're applying to Google's search infrastructure team or working on compiler design, when was the last time you implemented a red-black tree? Most of us use the data structures our language and framework provide. That's not laziness; it's **engineering judgement**.

These tests measure:
- How recently you did your Computer Science degree
- How much time you've spent on LeetCode preparing for interviews
- Whether you can code under pressure while someone watches

What they don't measure: Can you read documentation? Can you understand a complex codebase? Can you make architectural decisions? Can you debug real-world problems?

## Live Coding: Performance Theatre

"Write this function while I watch you." "Let's pair program on this problem." "Code up this feature in 45 minutes."

Live coding creates cognitive load that has nothing to do with actual work:
- The observer effect (someone watching you makes you worse)
- Time pressure that doesn't exist in real work
- No access to documentation or Stack Overflow (which you'd use in reality)
- Unfamiliar IDE/environment
- Stress that shuts down your working memory

For neurodivergent candidates—including many brilliant engineers—this approach is particularly cruel. Executive function issues, sensory sensitivities, and anxiety disorders all make live performance dramatically harder than actual work.

You're not seeing their best work. You're seeing their **stressed, observed, time-pressured** work. Why would you hire based on that?

## The GitHub Portfolio Fallacy

"Just show me your GitHub." Sounds reasonable, right? It's not.

Many excellent developers have empty GitHub profiles because:
- **NDAs**: Their best work is proprietary
- **Families**: They have children, aging parents, or other responsibilities that consume non-work hours
- **Other interests**: Shocking, but some developers have lives outside coding
- **Burnout prevention**: Not coding in free time is healthy, actually
- **Different contribution patterns**: They contribute to internal company projects, write documentation, mentor juniors—none of which shows up on GitHub

Requiring a GitHub portfolio selects for:
- Junior developers with lots of free time
- People without caring responsibilities
- Those whose economic situation allows unpaid labour
- Individuals whose neurodivergence manifests as hyperfocus on coding

It actively **excludes** experienced developers with families, caring responsibilities, or healthy work-life boundaries. Is that really your hiring strategy?

# The Interview Method That Actually Works

Here's the approach I've used successfully for years:

> **"Show me code you're proud of—and let's talk through it."**

That's it. That's the core of it.

## How It Works

**Before the interview** (with 5+ days notice):
- Tell the candidate you'll ask them to talk about a piece of code they've written
- Clarify: not a huge project, just code they can discuss
- Explain: you're interested in their thinking, not perfection
- Reassure: it doesn't need to be innovative or open source

**During the interview**:
- Ask them to share their screen and walk through the code
- Let them explain their approach
- Ask questions about their decisions
- Dig into edge cases, error handling, tradeoffs
- Explore related topics: testing, deployment, architecture

**Why this works**:

| Benefit | Outcome |
|---------|---------|
| **Low stress** | Talking about familiar code is comfortable |
| **Real work** | You see actual code they write, not performance theatre |
| **Adaptable depth** | You can adjust complexity based on their responses |
| **Branching discussions** | Architecture, testing, data flow—follow natural threads |
| **Authenticity check** | If they can't discuss code they supposedly wrote, that's a signal |
| **Context matters** | You see their reasoning: "We chose X because of constraint Y" |

## Adjusting for Seniority

**Junior developers**: Might not have code they can share. That's fine. Give them a small take-home exercise (2-3 hours max), then discuss it. Or provide a simple codebase and ask them to explain what it does. Focus on learning ability, not existing knowledge.

**Mid-level developers**: Should be able to discuss code they've written. Ask about testing, deployment, team collaboration. How did they handle feedback? What would they change now?

**Senior developers**: Expect architectural thinking. Why this approach versus alternatives? How does it scale? What are the failure modes? How did it evolve? What would you do differently?

The same basic method adapts across experience levels. You're always evaluating: Can this person think clearly about code? Can they articulate tradeoffs? Can they learn?

## What You Learn

This approach reveals things algorithmic tests can't:

**Technical judgement**: Why did they choose library X over library Y? What constraints informed that decision?

**Real constraints**: "We had to support IE11" or "The API had a 100-request-per-minute limit" shows real-world problem-solving.

**Evolution**: "I'd refactor this now because I learned X" demonstrates growth mindset.

**Testing approach**: "We tested it by..." reveals their quality standards.

**Team dynamics**: "We debated between two approaches..." shows collaboration.

**Error handling**: "We handle failures by..." indicates production thinking.

You're not testing trivia. You're evaluating engineering maturity.

# Agile Principles Applied to Interviewing

The [Agile Manifesto](https://agilemanifesto.org/) says:

> **Individuals and interactions** over processes and tools
> **Working software** over comprehensive documentation
> **Responding to change** over following a plan

Let's map these to interviewing:

## Individuals and Interactions Over Process and Tools

Traditional interviews prioritise **process**:
- Standard questions everyone gets
- Algorithmic tests with "correct" answers
- Scored rubrics that reduce humans to numbers

Agile interviews prioritise **interactions**:
- Conversations about real work
- Adapting questions based on responses
- Evaluating thought process, not just correctness

The interaction reveals more than any standardised process. A conversation about code they know lets you explore depth naturally. You follow interesting threads. You adjust complexity based on their responses.

You're assessing a person, not scoring a test.

## Working Software Over Documentation

Traditional interviews focus on **theoretical knowledge**:
- Can you recite the SOLID principles?
- Can you implement quicksort on a whiteboard?
- Can you explain Big-O notation?

Agile interviews focus on **working software**:
- Show me code that actually runs
- Walk me through how it works
- Tell me about problems you solved

Theory matters, but not in isolation. If someone can build robust systems but can't recite design patterns by name, that's fine. I can teach vocabulary; I can't teach thinking.

## Responding to Change Over Following a Plan

Traditional interviews are **rigid**:
- Same questions for every candidate
- Fixed time slots for each section
- No deviation from the script

Agile interviews are **adaptive**:
- Follow interesting technical threads
- Spend more time on areas relevant to the role
- Skip sections that clearly aren't relevant

If a candidate mentions they rebuilt your company's entire deployment pipeline, spend time on that. Don't force them through your pre-planned questions about CSS specifics.

The interview should flex to reveal their strengths, not force them into artificial constraints.

# ASD and Neuro-Inclusive Interview Design

I'm slightly autistic. Interviews have always been harder for me than actual work. The ambiguity, the social performance, the unwritten rules—exhausting.

Over the years I've refined an approach that works for neurodivergent minds. Turns out it works better for **everyone**.

## Clarity Reduces Anxiety

Autistic brains (and anxious brains, and anyone unfamiliar with tech interviews) hate ambiguity. Uncertainty creates cognitive load that blocks actual thinking.

So eliminate uncertainty:

**Before scheduling**:
- This will be a 60-minute video call
- You'll need your camera on (or not, if async options exist)
- We'll ask you to share your screen and walk through code
- No algorithmic puzzles, no whiteboarding, no brain teasers
- Bring code you're comfortable discussing—doesn't need to be fancy

**In the invitation**:
- Who will be in the interview (names, roles)
- What we'll cover (code discussion, architecture questions, team fit)
- What happens next (timeline, next steps, decision process)

This **reduces anxiety** and **activates competence**. When people know what to expect, they perform better.

It's not kindness. It's **performance optimisation**.

## Predictable Structure

Neurodivergent brains often struggle with context switching and ambiguous transitions. "Let's move on" can be jarring if you're mid-thought.

Provide structure:

**Opening (5 minutes)**:
- Introductions
- Overview of format
- "We'll spend 40 minutes discussing your code, 10 minutes on team questions, and 5 minutes for your questions. Sound good?"

**Main section (40 minutes)**:
- Code walkthrough
- Architecture discussion
- Edge cases and testing

**Team fit (10 minutes)**:
- How do you prefer to receive feedback?
- How do you handle disagreement with teammates?
- What environment helps you do your best work?

**Closing (5 minutes)**:
- Your questions for us
- Next steps and timeline

When people know where they are in the interview, they're less anxious about what's coming next.

## Accommodations Without Asking

Many neurodivergent people won't ask for accommodations because:
- They don't have a formal diagnosis
- They fear discrimination
- They don't know what's allowed
- They're masking and don't want to reveal it

So build accommodations into the default process:

**Async options**: Offer to send questions in advance, allow recorded responses
**Sensory considerations**: Let them turn off their camera if that helps
**Processing time**: "Take your time" is genuinely meant, not just politeness
**No surprise questions**: What you'll cover is in the invitation

These help neurodivergent candidates, but they **don't hurt neurotypical ones**. Everyone benefits from clarity and structure.

# Meeting Hygiene as Cognitive Hygiene

At Microsoft, I learned a principle that changed how I think about meetings:

> **No meeting without a spec, a note-taker, and a deliverable.**

Why? Because **if a decision isn't recorded, it didn't happen.**

This applies to interviews too.

## Before the Interview: The Spec

**What are we hiring for?** Not just "senior developer." What specific skills, experience, or perspective does the team need?

**What will we evaluate?** Technical ability, yes, but also: communication, collaboration, problem-solving approach, learning ability.

**What's our process?** How many interviews? Who's involved? What does each person evaluate?

Without this clarity, you get inconsistent evaluation. One interviewer focuses on algorithms, another on architecture, another on "culture fit" (which often means "people I'd have a beer with"—introducing bias).

Define what you're evaluating. Ensure interviewers align on it.

## During the Interview: The Note-Taker

One interviewer asks questions and engages. Another takes notes.

Why? Because **you can't fully engage in conversation while capturing detail**. You miss nuance, or you miss noting what they said.

Notes should capture:
- What they demonstrated (not just "good" or "bad")
- Specific examples they shared
- Questions they asked
- How they approached problems

Not "seemed smart" or "didn't vibe with them." Concrete observations.

## After the Interview: The Deliverable

Interviewers should write up their assessment immediately. Delay even a few hours and memory fades.

The deliverable:
- What you observed (factual)
- How it relates to the role requirements (evaluative)
- Your recommendation (hire, no hire, unsure)
- Specific reasoning

"They were great" is not useful. "They demonstrated strong architectural thinking when explaining how they designed the caching layer to handle cache invalidation across distributed nodes, showing awareness of consistency tradeoffs" is useful.

This creates **shared state**. Everyone deciding on the hire reads the same observations. You reduce bias and improve decisions.

# The Outcome: Reputation and Respect

Here's something surprising: **Candidates often email thanks even when rejected.**

Why? Because the process respected them as humans.

- They knew what to expect
- They weren't subjected to performance theatre
- They had a genuine technical conversation
- Rejection came with feedback ("We're looking for more distributed systems experience" vs. "We've decided to move forward with other candidates")

This matters because:

**Rejected candidates tell others**. If your interview process is humane and fair, people talk about it positively. You build a reputation that attracts talent.

**People change jobs**. That person you rejected today might be perfect in two years. If they remember the process fondly, they'll apply again.

**Hiring is marketing**. Every candidate is a potential customer, partner, or referrer. Treating them well pays dividends beyond the immediate hire.

Warmth + structure = strong engineering signal + strong reputation.

# Practical Implementation

Let's make this concrete. Here's how to run this style of interview.

## The Invitation Template

```
Subject: Interview for [Role] at [Company] - [Date/Time]

Hi [Name],

We'd like to schedule a technical interview for the [Role] position. Here's what to expect:

**Format**: 60-minute video call via [Zoom/Teams/Meet]
**Participants**: [Interviewer names and roles]
**Structure**:
- 5 minutes: Introductions and overview
- 40 minutes: Discussion of code you've written
- 10 minutes: Team and role questions
- 5 minutes: Your questions for us

**What to prepare**:
Please have ready a piece of code you've written that you're comfortable discussing. This could be:
- A feature you implemented at work (if not under NDA)
- A personal project
- A coding exercise or take-home from another interview
- An open source contribution

We're interested in your thinking and approach, not the complexity or innovation of the code. Even a simple utility function is fine if you can talk about why you wrote it that way.

**What we won't do**:
- No brain teasers or logic puzzles
- No whiteboard algorithm challenges
- No surprise technical tests

**Next steps**:
After the interview, we'll make a decision within [timeframe] and provide feedback regardless of outcome.

Please let us know if you need any accommodations to do your best in the interview.

Looking forward to talking with you!

[Your name]
```

## The Interview Script

**Opening (5 minutes)**:

"Thanks for joining us. I'm [name, role], and this is [name, role]. Here's how we'll spend the next hour:

- First, you'll walk us through some code you've written
- We'll ask questions about your approach, decisions, and tradeoffs
- Then we'll discuss how you work with teams and what you're looking for
- Finally, you'll have time to ask us anything

Does that sound good? Any questions before we start?"

**Main Discussion (40 minutes)**:

"Could you share your screen and show us the code you brought? Walk us through what it does and why you built it this way."

Then let them talk. Let them lead. Ask questions that follow naturally:
- "Why did you choose [X] over [Y]?"
- "How did you handle [edge case]?"
- "How did you test this?"
- "If you were writing this today, would you change anything?"
- "What constraints did you face?"

Branch into deeper topics as appropriate for seniority:
- Architecture: "How does this fit into the larger system?"
- Performance: "Did you have performance requirements?"
- Team: "How did others on your team interact with this code?"

**Team Fit (10 minutes)**:

"Let's talk about how you like to work:
- How do you prefer to receive feedback on your code?
- Tell me about a time you disagreed with a teammate. How did you handle it?
- What kind of environment helps you do your best work?"

**Closing (5 minutes)**:

"What questions do you have for us?"

Then clearly state next steps:
"We'll discuss internally and get back to you by [date] with a decision and feedback. Thanks for your time today."

## The Evaluation Template

After the interview, each interviewer completes this immediately:

```
**Candidate**: [Name]
**Role**: [Position]
**Interviewer**: [Your name]
**Date**: [Date]

**Technical Assessment**:

[Specific observations about their code, architecture thinking, problem-solving]

**Communication**:

[How clearly did they explain their thinking?]

**Experience Relevance**:

[How well does their experience align with role needs?]

**Learning and Growth**:

[Evidence of learning from experience, openness to feedback]

**Concerns**:

[Specific concerns, if any]

**Recommendation**:

[ ] Strong hire - Exceeds requirements
[ ] Hire - Meets requirements
[ ] Borderline - Mixed signals
[ ] No hire - Doesn't meet requirements

**Reasoning**:

[Specific justification for your recommendation]
```

This forces concrete thinking, not gut feeling.

# Common Objections

"But we need to know they can code!"

**Response**: Asking them to discuss code they've written demonstrates they can code. If they can't explain code they supposedly wrote, that's a signal.

---

"What if they just show us someone else's code?"

**Response**: In 15 years of interviewing this way, this has happened exactly zero times. When you ask detailed questions about decisions, tradeoffs, and evolution, it's immediately obvious if they didn't write it. Plus, if someone successfully fakes deep understanding of code they didn't write, they probably have the skills you need anyway.

---

"This doesn't scale for high-volume hiring."

**Response**: If you're hiring hundreds of developers, you have different problems. For most companies hiring dozens of engineers per year, this approach works fine. And it reduces false negatives (rejecting good candidates), which is expensive.

---

"Our team wants to see them code."

**Response**: You are seeing them code. Code they've written under normal working conditions. Why is watching them code under stress more valuable?

---

"We use algorithms questions to establish a baseline."

**Response**: A baseline of what? Ability to cram LeetCode? There are better baselines: Can they explain their thinking? Can they discuss tradeoffs? Can they learn?

---

"What about assessing problem-solving?"

**Response**: Ask them about problems they've solved. "Tell me about a difficult bug you debugged" or "Describe a time you had to optimise for performance" reveals problem-solving ability without artificial puzzles.

# Conclusion

Most technical interviews measure stress response, not engineering skill. We can do better.

A better approach exists:
- Let candidates discuss code they're proud of
- Examine real decisions, real constraints, real thinking
- Create low-stress conditions that reveal actual ability
- Provide clarity that reduces anxiety
- Respect candidates as human beings

This aligns with agile principles: individuals over process, working software over documentation, responding to change over rigid plans.

It's particularly beneficial for neurodivergent candidates, but it improves outcomes for everyone. Clarity, structure, and respect aren't accommodations; they're **performance optimisers**.

The outcome: You assess real engineering ability, build a positive reputation, and create an experience candidates remember fondly even if you don't hire them.

When interview processes respect **real humans** and **real engineering**, they don't just find good developers—they **attract them**.

We don't need trick questions. We need clarity, ownership, and curiosity—the same things good code needs.

---

*Have thoughts on interviewing practices? I'd love to hear them. These approaches evolved through years of iteration—which is very much in the agile spirit.*
