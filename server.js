import express from 'express';
import sqlite3 from 'sqlite3';
import cors from 'cors';

const app = express();
const db = new sqlite3.Database('./tasks.db');
app.use(cors());
app.use(express.json());


// Create tasks table
db.run(`
  CREATE TABLE IF NOT EXISTS tasks (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    text TEXT NOT NULL,
    completed BOOLEAN DEFAULT 0,
    sessionId TEXT NOT NULL
  )
`);

// Get tasks for a session
app.get('/api/tasks/:sessionId', (req, res) => {
  const { sessionId } = req.params;
  db.all('SELECT * FROM tasks WHERE sessionId = ?', [sessionId], (err, rows) => {
    if (err) {
      res.status(500).json({ error: err.message });
      return;
    }
    res.json(rows);
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

// Toggle task completion
app.put('/api/tasks/:id', (req, res) => {
  const { id } = req.params;
  const { completed } = req.body;
  db.run('UPDATE tasks SET completed = ? WHERE id = ?', [completed, id], (err) => {
    if (err) {
      res.status(500).json({ error: err.message });
      return;
    }
    res.json({ id, completed });
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
