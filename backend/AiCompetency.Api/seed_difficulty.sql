UPDATE "Questions" 
SET "DifficultyLevel" = floor(random() * 10 + 1)::int 
WHERE "DifficultyLevel" IS NULL;

UPDATE "Questions" 
SET "Status" = 2 
WHERE "Status" != 2;
