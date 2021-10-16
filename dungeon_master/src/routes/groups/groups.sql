/* @name GetByCode */
SELECT dungeon,player_number
FROM players_with_count
WHERE dungeon_code = :code
ORDER BY player_number;

/* @name InsertPlayerToGroup */
insert into players (group_id,code)
select id, :code as code from dungeons where code = :dungeonCode;

/* @name InsertGroup */
INSERT INTO dungeons (code)
VALUES (:code);

/* @name SubmitDungeon */
UPDATE players
SET dungeon=:dungeon
WHERE :dungeon_code IN (
    SELECT
        dungeons.code
    FROM dungeons
    INNER JOIN players
    ON 
    	players.group_id = dungeons.id
    WHERE dungeons.code = :dungeon_code
    and players.code = :player_code
    LIMIT 1
);
/* @name EveryoneSubmitted */
SELECT 
    done_count.done_count = total_count.total_count as isDone
FROM dungeons
INNER JOIN (
    SELECT
        count(*) as done_count,
        dungeons.id
    FROM players
    INNER JOIN dungeons
    on dungeons.id = players.group_id
    WHERE players.dungeon is null
    AND dungeons.code = :code
    GROUP BY dungeons.id
) as done_count
ON done_count.id = dungeons.id
INNER JOIN (
    SELECT
        count(*) as total_count,
        dungeons.id
    FROM players
    INNER JOIN dungeons
    ON dungeons.id = players.group_id
    WHERE dungeons.code = :code
    GROUP BY dungeons.id
) as total_count
ON total_count.id = dungeons.id;

/* @name finished_dungeon */
UPDATE players
SET dungeon=null
WHERE :dungeon_code IN (
    SELECT
        dungeons.code
    FROM dungeons
    INNER JOIN players
    ON 
    	players.group_id = dungeons.id
    WHERE dungeons.code = :dungeon_code
    and players.code = :player_code
    LIMIT 1
);