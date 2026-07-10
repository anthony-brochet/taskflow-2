# Tests à ajouter pour un projet complet

Cases à cocher au fur et à mesure. Fil conducteur du projet : le **cloisonnement
par utilisateur (IDOR)** — chaque service qui manipule des données d'un user doit
prouver qu'un user ne peut pas toucher celles d'un autre.

## Déjà couvert ✅
- `TaskService`, `TaskApiController`, `TodoController`, `JwtTokenService`, `TodoTask`

---

## 🔴 Priorité 1 — `AttachmentService` (le plus critique, non testé)
- [ ] `UploadAsync` : rejet si fichier null / vide
- [ ] `UploadAsync` : rejet si taille > limite
- [ ] `UploadAsync` : rejet si extension / type non autorisé
- [ ] `UploadAsync` : refus si la tâche n'appartient pas au `userId`
- [ ] `UploadAsync` : nom de fichier généré safe (pas de path traversal `../`)
- [ ] `UploadAsync` : succès → fichier écrit + entité persistée
- [ ] `GetForDownloadAsync` : `null` si l'attachement est à un autre user (IDOR)
- [ ] `GetForDownloadAsync` : renvoie le bon fichier sinon
- [ ] `DeleteAsync` : refus cross-user (IDOR)
- [ ] `DeleteAsync` : suppression fichier physique + entité
- [ ] `DeleteAsync` : comportement si fichier physique déjà absent

## 🔴 Priorité 2 — `AuthApiController.Login`
- [ ] Bons identifiants → `200` + token JWT
- [ ] Mauvais mot de passe / user inexistant → `401`
- [ ] Pas de fuite d'info (même réponse pour user inconnu et mauvais mdp)

## 🟠 Priorité 3 — `CommentService`
- [ ] `AddAsync` : refus si la tâche n'est pas au user (`false`)
- [ ] `AddAsync` : ajout OK sinon
- [ ] `DeleteAsync` : refus cross-user (IDOR)
- [ ] `DeleteAsync` : suppression OK
- [ ] `DeleteAsync` : comportement si commentaire inexistant

## 🟠 Priorité 4 — `CategoryService`
- [ ] `UpdateAsync` : `false` si id inexistant
- [ ] `UpdateAsync` : mise à jour OK
- [ ] `CreateAsync` : catégorie persistée
- [ ] `DeleteAsync` : suppression OK
- [ ] `GetByIdWithTasksAsync` : charge bien les tâches liées

## 🟡 Priorité 5 — Contrôleurs restants (codes retour)
- [ ] `AttachmentController` : `NotFound` / upload invalide → `BadRequest`
- [ ] `CommentController` : `NotFound`, refus cross-user
- [ ] `CategoryController` : `NotFound`, création / édition invalide
