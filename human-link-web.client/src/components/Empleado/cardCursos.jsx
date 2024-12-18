/* eslint-disable no-unused-vars */
import React, { useState } from "react";
import cursosService from "../../services/cursosService";
import { useEffect } from "react";

const CardCursos = () => {
    // Array inicial de cursos
    const [cursos, setCursos] = useState([]);

    useEffect(() => {
        cursosService.getCursosEmpleado()
            .then(response => {
                console.log("Response from getCursosEmpleado:", response);
                setCursos(response);
            })
            .catch(error => {
                console.error('Error al obtener los cursos:', error);
            });
    }, []);

    useEffect(() => {
        console.log("Cursos actualizados:", cursos);
    }, [cursos]);

    const [newCurso, setNewCurso] = useState({
        nombrecurso: "",
        duracion: "",
        url: "",
        descripcion: ""
    });

    const [selectedCurso, setSelectedCurso] = useState(null);

    const handleInputChange = (e) => {
        setNewCurso({
            ...newCurso,
            [e.target.name]: e.target.value
        });
    };

    const handleAddCurso = () => {
        setCursos([...cursos, newCurso]);
        setNewCurso({
            nombrecurso: "",
            duracion: "",
            url: "",
            descripcion: ""
        });
    };

    return (
        <>
            {/* Bot�n A�adir curso */}
            <button type="button" className="btn btn-success mb-4" data-bs-toggle="modal" data-bs-target="#aniadirModal">
                <i className="bi bi-plus-circle"></i> A�adir curso
            </button>

            {/* Modal para a�adir un nuevo curso */}
            <div className="modal fade" id="aniadirModal" tabIndex="-1" aria-labelledby="exampleModalLabel" aria-hidden="true">
                <div className="modal-dialog">
                    <div className="modal-content">
                        <div className="modal-header">
                            <h1 className="modal-title fs-5" id="exampleModalLabel">Crea un nuevo curso</h1>
                            <button type="button" className="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                        </div>
                        <div className="modal-body">
                            <form>
                                <div className="mb-3">
                                    <label htmlFor="titulo" className="col-form-label">T�tulo:</label>
                                    <input type="text" className="form-control" id="titulo" name="titulo" value={newCurso.nombrecurso} onChange={handleInputChange} />
                                </div>
                                <div className="mb-3">
                                    <label htmlFor="descripcion" className="col-form-label">Descripci�n:</label>
                                    <textarea className="form-control" id="descripcion" name="descripcion" value={newCurso.descripcion} onChange={handleInputChange}></textarea>
                                </div>
                                <div className="mb-3">
                                    <label htmlFor="duracion" className="col-form-label">Duraci�n (Horas):</label>
                                    <input type="number" className="form-control" id="duracion" name="duracion" value={newCurso.duracion} onChange={handleInputChange} />
                                </div>
                                <div className="mb-3">
                                    <label htmlFor="url" className="col-form-label">Url de la imagen:</label>
                                    <input type="text" className="form-control" id="url" name="url" value={newCurso.url} onChange={handleInputChange} />
                                </div>
                            </form>
                        </div>
                        <div className="modal-footer">
                            <button type="button" className="btn btn-danger" data-bs-dismiss="modal">Cancelar</button>
                            <button type="button" className="btn btn-success" data-bs-dismiss="modal" onClick={handleAddCurso}>A�adir</button>
                        </div>
                    </div>
                </div>
            </div>

            {/* tarjetas de los cursos */}
            <div className="row">
                {cursos.map((curso, index) => (
                    <div key={index} className="col-md-4 mb-4">
                        <div className="card h-100">
                            <img src={curso.Url && curso.Url.length > 0 ? curso.Url[0] : "imagen no encontrada"} className="card-img-top" alt={curso.Nombrecurso} />
                            <div className="card-body">
                                <h5 className="card-title">{curso.Nombrecurso}</h5>
                                <button
                                    type="button"
                                    className="btn btn-primary"
                                    data-bs-toggle="modal"
                                    data-bs-target="#infoModal"
                                    onClick={() => setSelectedCurso(curso)}
                                >
                                    Saber m�s
                                </button>
                            </div>
                        </div>
                    </div>
                ))}
            </div>

            {/* detalles del curso */}
            {selectedCurso && (
                <div className="modal fade" id="infoModal" tabIndex="-1" aria-labelledby="exampleModalLabel" aria-hidden="true">
                    <div className="modal-dialog modal-dialog-centered">
                        <div className="modal-content">
                            <div className="modal-header">
                                <h5 className="modal-title" id="exampleModalLabel">{selectedCurso.Nombrecurso}</h5>
                                <button type="button" className="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                            </div>
                            <div className="modal-body">
                                {selectedCurso.Descripcion}
                            </div>
                            <div className="modal-footer">
                                <button type="button" className="btn btn-secondary" data-bs-dismiss="modal">Cerrar</button>
                            </div>
                        </div>
                    </div>
                </div>
            )}
        </>
    );
};

export default CardCursos;