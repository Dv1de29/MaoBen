import { useEffect, useState } from 'react';
import './App.css'; // Asigură-te că există sau șterge linia dacă nu ai css

function App() {
  const [users, setUsers] = useState([]);
  const [nume, setNume] = useState("");
  const [email, setEmail] = useState("");

  // 1. Funcție pentru a încărca userii din Backend
  const fetchUsers = async () => {
    try {
      // URL-ul backend-ului din Docker (localhost:5000)
      const response = await fetch('http://localhost:5000/api/users');
      const data = await response.json();
      setUsers(data);
    } catch (error) {
      console.error("Eroare la fetch:", error);
    }
  };

  // 2. Încărcăm userii când pagina se deschide
  useEffect(() => {
    fetchUsers();
  }, []);

  // 3. Funcție pentru a adăuga un user nou
  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      const response = await fetch('http://localhost:5000/api/users', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name: nume, email: email }) // Atentie la litere mici/mari exact ca in C# User.cs
      });
      
      if (response.ok) {
        setNume(""); // Golim formularul
        setEmail("");
        fetchUsers(); // Reîncărcăm lista
      }
    } catch (error) {
      console.error("Eroare la postare:", error);
    }
  };

  return (
    <div style={{ padding: "20px", fontFamily: "Arial" }}>
      <h1>Lista Utilizat (din SQL Server)</h1>

      {/* Formular Adaugare */}
      <div style={{ marginBottom: "20px", border: "1px solid #ccc", padding: "10px" }}>
        <h3>Adaugă Utilizator</h3>
        <form onSubmit={handleSubmit}>
          <input 
            type="text" 
            placeholder="Nume" 
            value={nume}
            onChange={(e) => setNume(e.target.value)}
            style={{ marginRight: "10px" }}
          />
          <input 
            type="email" 
            placeholder="Email" 
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            style={{ marginRight: "10px" }}
          />
          <button type="submit">Salvează</button>
        </form>
      </div>

      {/* Lista Afisare */}
      <ul>
        {users.map((user) => (
          <li key={user.id}>
            <strong>{user.name}</strong> - {user.email}
          </li>
        ))}
      </ul>
      
      {users.length === 0 && <p>Nu există utilizatori în bază.</p>}
    </div>
  );
}

export default App;