import { useState, useEffect, useCallback } from 'react'
import axios from 'axios'
import './App.css'

const API_URL = import.meta.env.VITE_API_URL;

function App() {
  const [timeLeft, setTimeLeft] = useState(25 * 60) // 25 minutes in seconds
  const [isRunning, setIsRunning] = useState(false)
  const [mode, setMode] = useState('pomodoro') // pomodoro, shortBreak, longBreak
  const [tasks, setTasks] = useState([])
  const [newTask, setNewTask] = useState('')
  const [sessionId, setSessionId] = useState(null)
  const [sessions, setSessions] = useState([])
  const [showHistory, setShowHistory] = useState(false)
  const [expandedSession, setExpandedSession] = useState(null)
  const [sessionTasks, setSessionTasks] = useState({})

  const times = {
    pomodoro: 25 * 60,
    shortBreak: 5 * 60,
    longBreak: 15 * 60
  }

  const formatTime = (seconds) => {
    const mins = Math.floor(seconds / 60)
    const secs = seconds % 60
    return `${mins.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`
  }

  const resetTimer = useCallback(() => {
    setTimeLeft(times[mode])
    setIsRunning(false)
  }, [mode])

  const toggleTimer = async () => {
    if (!isRunning) {
      const newSessionId = Date.now().toString();
      setSessionId(newSessionId);
      setTasks([]); // Clear previous tasks
      try {
        await axios.post(`${API_URL}/sessions`, {
          sessionId: newSessionId,
          mode
        });
        fetchSessions();
      } catch (error) {
        console.error('Error creating session:', error);
      }
    }
    setIsRunning(!isRunning);
  }

  const fetchSessions = async () => {
    try {
      const response = await axios.get(`${API_URL}/sessions`);
      setSessions(response.data);
    } catch (error) {
      console.error('Error fetching sessions:', error);
    }
  };

  useEffect(() => {
    fetchSessions();
  }, []);

  const changeMode = (newMode) => {
    setMode(newMode)
    setTimeLeft(times[newMode])
    setIsRunning(false)
  }

  useEffect(() => {
    let interval
    if (isRunning && timeLeft > 0) {
      interval = setInterval(() => {
        setTimeLeft((time) => time - 1)
      }, 1000)
    } else if (timeLeft === 0) {
      setIsRunning(false)
      // Play notification sound or show notification here
    }
    return () => clearInterval(interval)
  }, [isRunning, timeLeft])

  useEffect(() => {
    const fetchTasks = async () => {
      if (!sessionId) return;
      try {
        const response = await axios.get(`${API_URL}/tasks/${sessionId}`);
        setTasks(response.data);
      } catch (error) {
        console.error('Error fetching tasks:', error);
      }
    };
    fetchTasks();
  }, [sessionId]);

  const addTask = async (e) => {
    e.preventDefault();
    if (!newTask.trim()) return;
    
    try {
      const response = await axios.post(`${API_URL}/tasks`, {
        text: newTask,
        sessionId
      });
      setTasks([...tasks, response.data]);
      setNewTask('');
    } catch (error) {
      console.error('Error adding task:', error);
    }
  };

  const deleteTask = async (taskId) => {
    try {
      await axios.delete(`${API_URL}/tasks/${taskId}`);
      setTasks(tasks.filter(task => task.id !== taskId));
    } catch (error) {
      console.error('Error deleting task:', error);
    }
  };

  const toggleTask = async (taskId, completed) => {
    try {
      await axios.put(`${API_URL}/tasks/${taskId}`, {
        completed: completed ? 0 : 1
      });
      setTasks(tasks.map(task => 
        task.id === taskId ? { ...task, completed: !task.completed } : task
      ));
    } catch (error) {
      console.error('Error toggling task:', error);
    }
  };

  return (
    <div className="timer-container">
      <div className="mode-controls">
        <button 
          className={`mode-button ${mode === 'pomodoro' ? 'active' : ''}`}
          onClick={() => changeMode('pomodoro')}
        >
          Pomodoro
        </button>
        <button 
          className={`mode-button ${mode === 'shortBreak' ? 'active' : ''}`}
          onClick={() => changeMode('shortBreak')}
        >
          Short Break
        </button>
        <button 
          className={`mode-button ${mode === 'longBreak' ? 'active' : ''}`}
          onClick={() => changeMode('longBreak')}
        >
          Long Break
        </button>
      </div>

      <div className="timer-display">
        {formatTime(timeLeft)}
      </div>

      <div className="timer-controls">
        <button className="timer-button" onClick={toggleTimer}>
          {isRunning ? 'Pause' : 'Start'}
        </button>
        <button className="timer-button secondary" onClick={resetTimer}>
          Reset
        </button>
      </div>

      <div className="tasks-container">
        <h3>Tasks</h3>
        <form onSubmit={addTask} className="task-form">
          <input
            type="text"
            value={newTask}
            onChange={(e) => setNewTask(e.target.value)}
            placeholder="Add a new task"
            className="task-input"
          />
          <button type="submit" className="task-button">Add</button>
        </form>
        <div className="tasks-list">
          {tasks.map(task => (
            <div key={task.id} className="task-item">
              <input
                type="checkbox"
                checked={task.completed}
                onChange={() => toggleTask(task.id, task.completed)}
              />
              <span className={task.completed ? 'completed' : ''}>
                {task.text}
              </span>
              <button 
                className="delete-button"
                onClick={(e) => {
                  e.preventDefault();
                  deleteTask(task.id);
                }}
              >
                ×
              </button>
            </div>
          ))}
        </div>
      </div>

      <div className="sessions-container">
        <button 
          className="history-button"
          onClick={() => setShowHistory(!showHistory)}
        >
          {showHistory ? 'Hide History' : 'Show History'}
        </button>
        
        {showHistory && (
          <div className="sessions-list">
            <h3>Previous Sessions</h3>
            {sessions.map(session => (
              <div key={session.id} className={`session-item ${expandedSession === session.id ? 'expanded' : ''}`}>
                <div className="session-header">
                  <span className="session-mode">{session.mode}</span>
                  <span className="session-date">
                    {new Date(session.startTime).toLocaleString()}
                  </span>
                </div>
                <div className="session-stats">
                  <div className="stats-row">
                    <span>Tasks: {session.completedTasks}/{session.totalTasks}</span>
                    <button 
                      className="view-tasks-button"
                      onClick={async () => {
                        if (expandedSession === session.id) {
                          setExpandedSession(null);
                        } else {
                          setExpandedSession(session.id);
                          try {
                            const response = await axios.get(`${API_URL}/tasks/${session.id}`);
                            setSessionTasks(prev => ({
                              ...prev,
                              [session.id]: response.data
                            }));
                          } catch (error) {
                            console.error('Error fetching session tasks:', error);
                          }
                        }
                      }}
                    >
                      {expandedSession === session.id ? 'Hide Tasks' : 'View Tasks'}
                    </button>
                  </div>
                  {expandedSession === session.id && sessionTasks[session.id] && (
                    <div className="session-tasks">
                      {sessionTasks[session.id].map((task) => (
                        <div key={task.id} className={`session-task ${task.completed ? 'completed' : ''}`}>
                          • {task.text}
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}

export default App
