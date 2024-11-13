import express from 'express';
import sqlite3 from 'sqlite3';
import cors from 'cors';

const app = express();
const db = new sqlite3.Database('./tasks.db');
app.use(cors());
app.use(express.json());


// Create tasks and sessions tables
db.run(`
  CREATE TABLE IF NOT EXISTS tasks (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    text TEXT NOT NULL,
    completed BOOLEAN DEFAULT 0,
    sessionId TEXT NOT NULL,
    completedAt DATETIME
  )
`);

db.run(`
  CREATE TABLE IF NOT EXISTS sessions (
    id TEXT PRIMARY KEY,
    startTime DATETIME DEFAULT CURRENT_TIMESTAMP,
    endTime DATETIME,
    mode TEXT
  )
`);

// Get tasks for a session
app.get('/api/tasks/:sessionId', (req, res) => {
  const { sessionId } = req.params;
  db.all('SELECT * FROM tasks WHERE sessionId = ? ORDER BY completedAt DESC', [sessionId], (err, rows) => {
    if (err) {
      res.status(500).json({ error: err.message });
      return;
    }
    res.json(rows);
  });
});

// Get all sessions with task counts
app.get('/api/sessions', (req, res) => {
  db.all(`
    SELECT 
      s.*,
      COUNT(t.id) as totalTasks,
      SUM(CASE WHEN t.completed = 1 THEN 1 ELSE 0 END) as completedTasks,
      GROUP_CONCAT(t.text, '||') as taskNames,
      GROUP_CONCAT(t.completed, '||') as taskCompletions
    FROM sessions s
    LEFT JOIN tasks t ON s.id = t.sessionId
    GROUP BY s.id
    ORDER BY s.startTime DESC
  `, [], (err, rows) => {
    if (err) {
      res.status(500).json({ error: err.message });
      return;
    }
    // Process the concatenated strings into arrays
    rows = rows.map(row => ({
      ...row,
      tasks: row.taskNames ? row.taskNames.split('||').map((text, index) => ({
        text,
        completed: row.taskCompletions.split('||')[index] === '1'
      })) : []
    }));
    // Remove the concatenated strings from the response
    rows.forEach(row => {
      delete row.taskNames;
      delete row.taskCompletions;
    });
    res.json(rows);
  });
});

// Start new session
app.post('/api/sessions', (req, res) => {
  const { sessionId, mode } = req.body;
  db.run('INSERT INTO sessions (id, mode) VALUES (?, ?)', [sessionId, mode], (err) => {
    if (err) {
      res.status(500).json({ error: err.message });
      return;
    }
    res.json({ sessionId, mode });
  });
});

// Add new task
app.post('/api/tasks', (req, res) => {
  const { text, sessionId } = req.body;
  db.run('INSERT INTO tasks (text, sessionId) VALUES (?, ?)', [text, sessionId], function(err) {
    if (err) {
      res.status(500).json({ error: err.message });
      return;
    }
    res.json({ id: this.lastID, text, completed: 0 });
  });
});

// Delete task
app.delete('/api/tasks/:id', (req, res) => {
  const { id } = req.params;
  db.run('DELETE FROM tasks WHERE id = ?', [id], (err) => {
    if (err) {
      res.status(500).json({ error: err.message });
      return;
    }
    res.json({ id });
  });
});

// Toggle task completion
app.put('/api/tasks/:id', (req, res) => {
  const { id } = req.params;
  const { completed } = req.body;
  const completedAt = completed ? new Date().toISOString() : null;
  db.run('UPDATE tasks SET completed = ?, completedAt = ? WHERE id = ?', [completed, completedAt, id], (err) => {
    if (err) {
      res.status(500).json({ error: err.message });
      return;
    }
    res.json({ id, completed, completedAt });
  });
});

const PORT = 3000;
app.listen(PORT, () => {
  console.log(`Server running on port ${PORT}`);
});

// Handle database errors
db.on('error', (err) => {
  console.error('Database error:', err);
});

// Log successful connection
db.on('open', () => {
  console.log('Connected to the tasks database.');
});
