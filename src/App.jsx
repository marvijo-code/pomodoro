import { useState, useEffect, useCallback } from 'react'
import './App.css'

function App() {
  const [timeLeft, setTimeLeft] = useState(25 * 60) // 25 minutes in seconds
  const [isRunning, setIsRunning] = useState(false)
  const [mode, setMode] = useState('pomodoro') // pomodoro, shortBreak, longBreak

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

  const toggleTimer = () => {
    setIsRunning(!isRunning)
  }

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
    </div>
  )
}

export default App
