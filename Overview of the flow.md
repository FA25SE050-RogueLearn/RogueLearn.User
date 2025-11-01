Overview of the flow

- Transaction 1: Extract your academic record (read-only). Endpoint: /api/academic-records/extract
- Onboarding: Select your academic route (program) and career class. Endpoint: /api/onboarding/complete
- Transaction 2: Process and persist your academic record; generate learning path and quests. Endpoint: /api/users/me/process-academic-record
- Optional: Analyze learning gap and forge learning path structure. Endpoints: /api/learning-paths/analyze-gap and /api/learning-paths/forge
- View your learning path. Endpoint: /api/learning-paths/me
All endpoints require authentication (controllers use [Authorize]).

Step 1) Extract academic record (read-only)

- Endpoint: POST /api/academic-records/extract
- Content type: multipart/form-data
- What it does:
  - Preprocesses the raw FAP HTML to isolate the grade report table using HtmlAgilityPack.
  - Calls IFapExtractionPlugin (Semantic Kernel prompt) to transform the cleaned text into a structured JSON that matches FapRecordData.
  - Does not persist anything; this is purely extraction/analysis.
- What it returns:
  - A FapRecordData object:
    - gpa: number (optional)
    - subjects: array of items with:
      - subjectCode: string (e.g., CSI104)
      - status: one of Passed, Failed, Studying
      - mark: number (optional, null if not available)
- What content to put in:
  - Form field fapHtmlContent: the raw HTML of your academic transcript page from FAP (copy-paste the HTML content that contains the grades table).
  - Example form fields:
    - fapHtmlContent = "...
      ...
       ..."
Step 2) Onboarding: choose academic route and class

- Get options:
  - GET /api/onboarding/routes returns available academic routes (curriculum programs).
  - GET /api/onboarding/classes returns available career specialization classes.
- Complete onboarding:
  - Endpoint: POST /api/onboarding/complete
  - Content type: application/json
- What it does:
  - Validates your selected CurriculumProgramId (route) and CareerRoadmapId (class).
  - Finds the latest active CurriculumVersion for the chosen program.
  - Ensures there’s a StudentEnrollment for you + that curriculum version (creates if needed).
  - Updates your profile with the chosen route and class; sets OnboardingCompleted = true.
- What it returns:
  - 204 No Content on success. The effect is persisted (profile + enrollment).
- What content to put in:
  - JSON body with:
    - CurriculumProgramId: GUID of the selected academic route (from GET /api/onboarding/routes).
    - CareerRoadmapId: GUID of the selected career class (from GET /api/onboarding/classes).
  - AuthUserId is inferred from your token; you do not send it.
  - Example JSON:
    - { "CurriculumProgramId": "GUID-of-program", "CareerRoadmapId": "GUID-of-class" }
Step 3) Process academic record and generate learning path

- Endpoint: POST /api/users/me/process-academic-record
- Content type: multipart/form-data
- What it does:
  - Preprocesses the raw FAP HTML to the grade table text (same approach as extract).
  - Caches extraction by SHA-256 of cleaned text:
    - Tries to reuse previous AI extraction if the same content was processed before.
    - If not cached, calls IFapExtractionPlugin and saves the latest extraction to Supabase storage under curriculum-imports/user-academic-records/{AuthUserId}/latest.
  - Deserializes the extracted JSON into FapRecordData.
  - Ensures a StudentEnrollment exists for you + the specified CurriculumVersionId.
  - For each subject in the official CurriculumStructure (for the selected curriculum version):
    - Creates or updates StudentSemesterSubject with status (Completed/Failed/Enrolled) and grade.
    - Creates or reuses a LearningPath (one per user per curriculum version).
    - Creates QuestChapter per semester (e.g., “Semester 1”) if missing.
    - Creates Quest per subject (Practice quest linked to the subject).
    - Links quests to the learning path with a SequenceOrder (to keep consistent ordering).
    - Sets UserQuestProgress per quest based on the subject status:
      - Passed → Completed
      - Failed/Studying → InProgress
      - Unknown → NotStarted
- What it returns:
  - ProcessAcademicRecordResponse with:
    - IsSuccess: boolean
    - Message: summary of the synchronization
    - LearningPathId: GUID of the created or existing learning path
    - SubjectsProcessed: number of subjects from FapRecordData
    - QuestsGenerated: number of new quests created
    - CalculatedGpa: value from the extracted record (0.0 if not present)
- What content to put in:
  - Form fields:
    - fapHtmlContent: your raw FAP HTML as in Step 1.
    - curriculumVersionId: the GUID of the curriculum version you are enrolled in.
      - Typically, after onboarding, you use the latest active version of your chosen program.
      - You can query program/version info from admin/internal endpoints or your own catalog (e.g., /api/internal/curriculum/programs/{id}/details) if needed.
  - Example form fields:
    - fapHtmlContent = "... transcript table ..."
    - curriculumVersionId = "GUID-of-latest-active-version-for-your-program"
Step 4) Optional: Analyze learning gap and forge learning path structure

- Analyze gap:
  - Endpoint: POST /api/learning-paths/analyze-gap
  - Content type: application/json
  - What it does: uses your verified FapRecordData to compute a high-level recommendation and a ForgingPayload.
  - What it returns: GapAnalysisResponse with fields:
    - recommendedFocus
    - highestPrioritySubject
    - reason
    - forgingPayload (subjectGaps: list of subject codes/topics to prioritize)
  - What content to put in:
    - Send a FapRecordData JSON you trust (from Step 1 or Step 3).
    - Example JSON:
      - { "gpa": 3.21, "subjects": [ { "subjectCode": "CSI104", "status": "Passed", "mark": 8.1 }, { "subjectCode": "PRF192", "status": "Studying", "mark": null } ] }
- Forge learning path:
  - Endpoint: POST /api/learning-paths/forge
  - Content type: application/json
  - What it does:
    - Checks you completed onboarding (must have RouteId).
    - Uses your latest active curriculum version for that route.
    - Creates a new LearningPath (if none exists for you + that curriculum version).
  - What it returns:
    - ForgedLearningPath with:
      - id, name, description
  - What content to put in:
    - Use the forgingPayload from gap analysis:
    - { "subjectGaps": [ "PRF192", "DBI202" ] }
Step 5) View your learning path

- Endpoint: GET /api/learning-paths/me
- What it does:
  - Aggregates your LearningPath, its QuestChapters, and Quests with their progress.
  - Calculates completion percentage.
- What it returns:
  - LearningPathDto with:
    - id, name, description
    - chapters: list of QuestChapterDto
      - id, title (“Semester N”), sequence (semester number), status
      - quests: list of QuestSummaryDto per chapter
        - id, title, status (NotStarted/InProgress/Completed), sequenceOrder, learningPathId, chapterId
    - completionPercentage
Notes and practical tips

- Authentication: All endpoints above require a valid token; server extracts your AuthUserId from claims.
- Content types:
  - Extraction and processing steps accept multipart/form-data for fapHtmlContent to handle large/complex strings.
  - Onboarding completion, gap analysis, and forging use JSON payloads.
- Where to get IDs:
  - CurriculumProgramId: from GET /api/onboarding/routes.
  - CareerRoadmapId (class): from GET /api/onboarding/classes.
  - CurriculumVersionId: the system uses the latest active version of the selected program during onboarding. For processing, you must send a specific curriculumVersionId; you can look it up through admin/internal endpoints or catalog queries if your UI doesn’t surface it yet.
- Status mapping:
  - FAP status “Passed” → student subject Completed; quest Completed.
  - “Failed”/“Studying” → student subject Failed/Enrolled; quest InProgress.
- Caching:
  - The processing step hashes the cleaned transcript text and tries to reuse cached extraction from Supabase storage. If cache misses, it runs extraction and saves the result for future requests.
Quick endpoint summary

- Extract (read-only): POST /api/academic-records/extract, form-data: fapHtmlContent → returns FapRecordData
- Onboarding:
  - GET /api/onboarding/routes → list of curriculum programs
  - GET /api/onboarding/classes → list of career classes
  - POST /api/onboarding/complete, JSON { CurriculumProgramId, CareerRoadmapId } → 204 No Content
- Process & persist learning path/quests: POST /api/users/me/process-academic-record, form-data { fapHtmlContent, curriculumVersionId } → returns ProcessAcademicRecordResponse
- Analyze gap: POST /api/learning-paths/analyze-gap, JSON FapRecordData → returns GapAnalysisResponse
- Forge path: POST /api/learning-paths/forge, JSON ForgingPayload → returns ForgedLearningPath
- View path: GET /api/learning-paths/me → returns LearningPathDto