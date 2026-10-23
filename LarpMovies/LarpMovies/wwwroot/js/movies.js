// Filtur (po vsichki poleta) za spisuka s filmi - izcialo v brauzura
(function () {
    'use strict';

    const app = document.getElementById('movies-app');
    if (!app) return;

    const els = {
        body: document.getElementById('movies-body'),
        filter: document.getElementById('movies-filter'),
        info: document.getElementById('movies-info')
    };

    const state = { all: [], filtered: [], terms: [] };

    // Poletata, po koito se tursi (tochno tezi, koito se vizhdat v tablicata).
    const fields = ['id', 'title', 'director', 'year', 'genre', 'rating', 'actors'];

    const escapeRegExp = s => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const norm = v => (v ?? '').toString().toLowerCase();

    function applyFilter() {
        state.terms = norm(els.filter.value).split(/\s+/).filter(Boolean);
        // Vsyaka duma trqbva da se sreshta v pone edno pole (logichesko I mezhdu dumite).
        state.filtered = state.all.filter(m => {
            const haystack = fields.map(f => norm(m[f]));
            return state.terms.every(t => haystack.some(h => h.includes(t)));
        });
        render();
    }

    function highlight(parent, text) {
        const value = (text ?? '').toString();
        if (!value) { parent.textContent = '\u2014'; return; }
        if (!state.terms.length) { parent.textContent = value; return; }
        const re = new RegExp('(' + state.terms.map(escapeRegExp).join('|') + ')', 'gi');
        // split sas zahvashtashta grupa vrushta suvpadeniyata na nechetnite pozicii
        value.split(re).forEach((part, i) => {
            if (!part) return;
            if (i % 2 === 1) {
                const mark = document.createElement('mark');
                mark.textContent = part;
                parent.appendChild(mark);
            } else {
                parent.appendChild(document.createTextNode(part));
            }
        });
    }

    function cell(tr, text) {
        const td = document.createElement('td');
        highlight(td, text);
        tr.appendChild(td);
        return td;
    }

    function renderRows(rows) {
        els.body.replaceChildren();
        if (!rows.length) {
            const tr = document.createElement('tr');
            const td = document.createElement('td');
            td.colSpan = 7;
            td.className = 'text-center text-muted py-4';
            td.textContent = state.all.length ? 'No movies match your search.' : 'There are no movies yet.';
            tr.appendChild(td);
            els.body.appendChild(tr);
            return;
        }
        rows.forEach(m => {
            const tr = document.createElement('tr');
            cell(tr, m.id);
            cell(tr, m.title);
            cell(tr, m.director);
            cell(tr, m.year);
            cell(tr, m.genre);
            cell(tr, m.rating);
            cell(tr, m.actors);
            els.body.appendChild(tr);
        });
    }

    function render() {
        renderRows(state.filtered);
        els.info.textContent = state.all.length
            ? `Showing ${state.filtered.length} of ${state.all.length}`
            : '';
    }

    function debounce(fn, ms) {
        let t;
        return (...args) => { clearTimeout(t); t = setTimeout(() => fn(...args), ms); };
    }

    els.filter.addEventListener('input', debounce(applyFilter, 150));

    fetch(app.dataset.listUrl, { headers: { Accept: 'application/json' } })
        .then(r => { if (!r.ok) throw new Error(r.statusText); return r.json(); })
        .then(data => { state.all = data; applyFilter(); })
        .catch(() => {
            els.body.replaceChildren();
            const tr = document.createElement('tr');
            const td = document.createElement('td');
            td.colSpan = 7;
            td.className = 'text-center text-danger py-4';
            td.textContent = 'Error loading the movies.';
            tr.appendChild(td);
            els.body.appendChild(tr);
        });
})();